using System.CommandLine;
using System.CommandLine.Invocation;
using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.HostRunners;
using Ashlar.Manifest;
using Ashlar.Manifest.Admission;
using Ashlar.Manifest.Packaging;
using Ashlar.Manifest.Signing;

namespace Ashlar.CLI.Commands;

/// <summary>
/// <c>ashlar pkg</c> — certified extension packages: admissions that travel.
///
/// <para><c>export</c> seals an ADMITTED extension (its files, its course evidence, and the
/// signed verdict) into a portable <c>.ashpkg</c>. <c>import</c> verifies one intrinsically —
/// no local keys, no network — and then submits it to THIS project's gate: the origin's
/// certification is evidence, never authority. Sealed here rejects it; proposing holds it for
/// this operator; self-extending admits within this project's budget. <c>show</c> inspects a
/// package without touching any project.</para>
///
/// <para><c>publish</c> and <c>pull</c> move packages through a mesh store, and <c>share</c> is
/// export + publish in one verb — all through the kernel's one door
/// (<see cref="MeshStore"/>), the same door a self-extend cycle's auto-share uses.</para>
/// </summary>
public sealed class PkgCommand : Command
{
    /// <summary>Creates a new PkgCommand instance.</summary>
    public PkgCommand() : base("pkg", "Export, inspect, share, and import certified extension packages (.ashpkg).")
    {
        AddCommand(BuildExport());
        AddCommand(BuildImport());
        AddCommand(BuildShow());
        AddCommand(BuildPublish());
        AddCommand(BuildPull());
        AddCommand(BuildShare());
    }

    private static Option<DirectoryInfo> PathOption() => new(
        name: "--path",
        description: "Project directory (defaults to current).",
        getDefaultValue: () => new DirectoryInfo(Environment.CurrentDirectory));

    // ─────────────────────────── export ───────────────────────────

    private static Command BuildExport()
    {
        var idOpt = new Option<string>("--id", "The admitted proposal to package.") { IsRequired = true };
        var outOpt = new Option<FileInfo>("--out", "Where to write the .ashpkg.") { IsRequired = true };
        var pathOpt = PathOption();
        var cmd = new Command("export", "Seal an admitted extension into a portable package.") { idOpt, outOpt, pathOpt };
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await ExportAsync(
                ctx.ParseResult.GetValueForOption(idOpt)!,
                ctx.ParseResult.GetValueForOption(outOpt)!,
                ctx.ParseResult.GetValueForOption(pathOpt)!);
        });
        return cmd;
    }

    private static async Task<int> ExportAsync(string id, FileInfo outFile, DirectoryInfo directory)
    {
        if (!File.Exists(Path.Combine(directory.FullName, "ashlar.yaml")))
        {
            Console.Error.WriteLine($"not an ashlar project: no ashlar.yaml in {directory.FullName}");
            return 1;
        }

        try
        {
            // Sealing requires the local operator identity — a package's seal is a signature,
            // and there is nothing honest to write without one.
            var sealer = OperatorKey.TryLoad();
            if (sealer is null)
            {
                Console.Error.WriteLine("exporting requires an operator key — the seal is a signature.");
                Console.Error.WriteLine("create one with:  ashlar keys init");
                return 1;
            }

            var (code, gathered, gatheredFiles) = await GatherAsync(id, directory);
            if (code != 0)
            {
                return code;
            }
            var (record, files) = (gathered!, gatheredFiles!);

            var json = ExtensionPackaging.Pack(record, files, sealer);
            await File.WriteAllTextAsync(outFile.FullName, json);

            Console.WriteLine($"  {Gold("✓ packaged")}  {record.Proposal.Summary}");
            Console.WriteLine($"  {Dim($"{files.Count} file(s) · admitted by {record.Actor} · verdict {Fp(record.Signer)} · seal {Fp(sealer.PublicKeyBase64)}")}");
            Console.WriteLine($"  {Dim($"→ {outFile.FullName}")}");
            return 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// The record and its files — the parked writes the gate admitted, straight from the forge
    /// queue. Shared by <c>export</c> and <c>share</c>: what travels must be identical however it
    /// leaves. On failure, prints the reason and returns the exit code with no record: 1 when the
    /// extension cannot be packaged whole, 65 when the rows fail the admission's signed content
    /// claims — a verification refusal, same family as a package that fails its seal.
    /// </summary>
    private static async Task<(int Code, GateRecord? Record, List<PackageFile>? Files)> GatherAsync(string id, DirectoryInfo directory)
    {
        var store = new GateStore(Path.Combine(directory.FullName, ".ashlar"));
        var record = await store.GetAsync(id);
        if (record is null)
        {
            Console.Error.WriteLine($"no proposal '{id}' in the store.");
            return (1, null, null);
        }

        var forge = AshlarProjectMediation.ProjectStore(directory.FullName);
        var files = new List<PackageFile>();
        foreach (var forgeId in record.Proposal.ForgeProposalIds)
        {
            var proposal = forge.Find(forgeId);
            if (proposal is null)
            {
                Console.Error.WriteLine($"forge proposal '{forgeId}' referenced by '{id}' is missing from the forge store — cannot package an incomplete extension.");
                return (1, null, null);
            }
            // An admitted record's rows are Applied. Anything else under this id is a row the
            // gate did not admit-and-apply (a shadow, a replacement) — refuse to seal it.
            if (proposal.Status != ChangeProposalStatus.Applied)
            {
                Console.Error.WriteLine($"forge proposal '{forgeId}' is {proposal.Status}, not Applied — only content the gate admitted AND applied may travel.");
                return (1, null, null);
            }
            files.Add(new PackageFile { Path = proposal.TargetPath, Content = proposal.NewContent });
        }
        // The rows just re-read are mutable disk; the claims inside the record are signed. When
        // the admission carries claims, the rows must hash to them — a row edited between
        // admission and packaging must not travel under the origin's signature.
        if (!ExtensionPackaging.VerifyFileClaims(record.Proposal, files, out var claimReason))
        {
            Console.Error.WriteLine(claimReason);
            return (65, null, null);
        }
        return (0, record, files);
    }

    // ─────────────────────────── import ───────────────────────────

    private static Command BuildImport()
    {
        var fileArg = new Argument<FileInfo>("package", "The .ashpkg to import.");
        var pathOpt = PathOption();
        var cmd = new Command("import", "Verify a package and submit it to THIS project's gate.") { fileArg, pathOpt };
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await ImportAsync(
                ctx.ParseResult.GetValueForArgument(fileArg),
                ctx.ParseResult.GetValueForOption(pathOpt)!);
        });
        return cmd;
    }

    // Read a .ashpkg with a size guard BEFORE the whole file lands in a string. A mesh store
    // is a plain synced directory (MeshStore's transport-naive model), so a .ashpkg there is
    // attacker-influenceable; an unbounded read-to-string of a several-hundred-MB planted file
    // is an OOM before ExtensionPackaging.TryOpen's own char cap ever runs. Refuse fail-closed.
    // Matches ExtensionPackaging's own parse ceiling: a .ashpkg over this is not a certified
    // extension. Kept as a local constant so this guard has no cross-package version coupling.
    private const long MaxPackageBytes = 16L * 1024 * 1024;

    // The ceiling test on its own, split out from the read. `publish` and `pull` read the file
    // asynchronously, so a guard welded to the synchronous read is one they cannot call — and a
    // guard only some read paths can reach is how those two stayed unbounded while `import` and
    // `show` were safe. One ceiling, one sentence, four callers.
    private static bool TryCheckPackageSize(FileInfo file, out string reason)
    {
        var length = file.Length;
        if (length > MaxPackageBytes)
        {
            reason = $"REFUSED: {file.Name} is {length:N0} bytes; the limit is "
                   + $"{MaxPackageBytes:N0}. "
                   + "A package this large is not a certified extension — refusing before reading it.";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    private static bool TryReadPackage(FileInfo file, out string json, out string reason)
    {
        json = string.Empty;
        if (!TryCheckPackageSize(file, out reason))
        {
            return false;
        }
        json = File.ReadAllText(file.FullName);
        return true;
    }

    private static async Task<int> ImportAsync(FileInfo file, DirectoryInfo directory)
    {
        var policyPath = Path.Combine(directory.FullName, "ashlar.policy.yaml");
        if (!File.Exists(Path.Combine(directory.FullName, "ashlar.yaml")) || !File.Exists(policyPath))
        {
            Console.Error.WriteLine($"not an ashlar project: {directory.FullName}");
            return 1;
        }
        if (!file.Exists)
        {
            Console.Error.WriteLine($"no such package: {file.FullName}");
            return 1;
        }

        // Verify + submit is shared with `mesh pull` — how a package arrived must not change how
        // it is admitted. A peek first, so the operator sees the origin's evidence before the verdict.
        if (!TryReadPackage(file, out var packageJson, out var readReason))
        {
            Console.Error.WriteLine(readReason);
            return 65;
        }
        if (!ExtensionPackaging.TryOpen(packageJson, out var peek, out var peekReason))
        {
            Console.Error.WriteLine(peekReason);
            return 65;
        }
        Console.WriteLine();
        Console.WriteLine($"  {peek!.Record.Proposal.Summary}");
        Console.WriteLine($"  {Dim($"origin verdict {Fp(peek.Record.Signer)} · seal {Fp(peek.SealSigner)} · {peek.Files.Count} file(s)")}");
        foreach (var course in peek.Record.Proposal.Courses)
        {
            var glyph = course.Passed ? Ok("✓") : Bad("×");
            Console.WriteLine($"  {glyph} {course.Name,-12} {Dim($"{course.Detail} (evidence from origin)")}");
        }
        Console.WriteLine();

        var result = await PackageImport.SubmitAsync(directory.FullName, packageJson);
        switch (result.Outcome)
        {
            case PackageAdmission.Admitted:
                Console.WriteLine($"  {Gold($"✓ ADMITTED — {result.Message}")}");
                foreach (var path in result.AppliedPaths)
                {
                    Console.WriteLine($"  {Ok("✓ applied")}  {path}");
                }
                if (result.Warning is not null)
                {
                    Console.Error.WriteLine($"  {Bad("! " + result.Warning)}");
                    return 1;   // admission recorded, but the disk state is not what was intended
                }
                return 0;
            case PackageAdmission.Held:
                Console.WriteLine($"  {Clay($"! HELD — {result.Message}")}");
                Console.WriteLine($"  {Dim("nothing is on disk. review:  ashlar gates --show " + result.LocalProposalId)}");
                return 0;
            case PackageAdmission.AlreadyImported:
                Console.WriteLine($"  {Dim("− already imported: " + result.Message)}");
                return 0;
            case PackageAdmission.Rejected:
                Console.WriteLine($"  {Bad($"× REJECTED — {result.Message}")}");
                Console.WriteLine($"  {Dim("disk untouched")}");
                return 65;
            case PackageAdmission.Refused:
                // A package that does not verify — an untrusted sealer, a policy that won't load — is a
                // 65 here, exactly as it is for `pkg pull` (PullAsync maps Refused to 65 too) and for the
                // early TryOpen peek above. How a package arrived must not change its exit code: a lone
                // import of an untrusted-signer package must not exit 1 while a pull of the same exits 65.
                Console.WriteLine($"  {Bad($"× REFUSED — {result.Message}")}");
                Console.WriteLine($"  {Dim("disk untouched")}");
                return 65;
            default:
                Console.Error.WriteLine(result.Message);
                return 1;
        }
    }

    // ─────────────────────────── show ───────────────────────────

    private static Command BuildShow()
    {
        var fileArg = new Argument<FileInfo>("package", "The .ashpkg to inspect.");
        var cmd = new Command("show", "Verify and describe a package without touching any project.") { fileArg };
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await ShowAsync(ctx.ParseResult.GetValueForArgument(fileArg));
        });
        return cmd;
    }

    private static async Task<int> ShowAsync(FileInfo file)
    {
        if (!file.Exists)
        {
            Console.Error.WriteLine($"no such package: {file.FullName}");
            return 1;
        }
        if (!TryReadPackage(file, out var showJson, out var showReadReason))
        {
            Console.Error.WriteLine(showReadReason);
            return 65;
        }
        if (!ExtensionPackaging.TryOpen(showJson, out var pkg, out var reason))
        {
            Console.Error.WriteLine(reason);
            return 65;
        }

        var r = pkg!.Record;
        Console.WriteLine();
        Console.WriteLine($"  {r.Proposal.Summary}");
        Console.WriteLine($"  {Dim($"{r.Proposal.Id} · kind {r.Proposal.Kind} · by {r.Proposal.ProposedBy} · {r.Proposal.ProposedAt:u}")}");
        Console.WriteLine();
        foreach (var course in r.Proposal.Courses)
        {
            var glyph = course.Passed ? Ok("✓") : Bad("×");
            Console.WriteLine($"  {glyph} {course.Name,-12} {Dim(course.Detail)}");
        }
        Console.WriteLine();
        foreach (var pf in pkg.Files)
        {
            Console.WriteLine($"  {Dim($"file · {pf.Path} · {pf.Content.Length} chars")}");
        }
        Console.WriteLine();
        Console.WriteLine($"  {Gold("✓ package verifies")}  {Dim($"verdict signed {Fp(r.Signer)} · sealed {Fp(pkg.SealSigner)} · admitted by {r.Actor}")}");
        Console.WriteLine($"  {Dim("importing runs it through YOUR gate:  ashlar pkg import <file>")}");
        return 0;
    }

    // ─────────────────────────── publish (to the mesh) ───────────────────────────

    private static Option<DirectoryInfo?> StoreOption() => new(
        name: "--store",
        description: "Mesh package store (defaults to $ASHLAR_MESH_DIR, else ~/.ashlar/mesh/published).");

    // Resolution and placement live in the kernel (MeshStore), so this verb and a self-extend
    // cycle's auto-share go through the same door with the same rule.
    private static string ResolveStore(DirectoryInfo? store) => MeshStore.Resolve(store?.FullName);

    private static Command BuildPublish()
    {
        var fileArg = new Argument<FileInfo>("package", "The .ashpkg to publish to the mesh.");
        var storeOpt = StoreOption();
        var cmd = new Command("publish", "Verify a package and place it in the mesh store for peers to pull.") { fileArg, storeOpt };
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await PublishAsync(
                ctx.ParseResult.GetValueForArgument(fileArg),
                ctx.ParseResult.GetValueForOption(storeOpt));
        });
        return cmd;
    }

    private static async Task<int> PublishAsync(FileInfo file, DirectoryInfo? store)
    {
        if (!file.Exists)
        {
            Console.Error.WriteLine($"no such package: {file.FullName}");
            return 1;
        }
        // Bound the file before it is buffered, the same ceiling `import` and `show` apply. A .ashpkg
        // handed to publish has routinely just arrived off a share — that is what a mesh is — so it is
        // no more trustworthy here than at import, and a scripted republish of a synced folder hands
        // this verb attacker-influenced bytes. TryOpen's own char cap is no backstop: it measures a
        // string that exists only once the unbounded read has already succeeded, or exhausted memory
        // trying, and an OutOfMemoryException is a crash where a refusal belongs.
        if (!TryCheckPackageSize(file, out var sizeReason))
        {
            Console.Error.WriteLine(sizeReason);
            return 65;
        }
        var json = await File.ReadAllTextAsync(file.FullName);

        // Never publish what does not verify — the mesh carries certified packages only, so a
        // forged one is refused at the source rather than propagated to every peer.
        if (!ExtensionPackaging.TryOpen(json, out var pkg, out var reason))
        {
            Console.Error.WriteLine(reason);
            return 65;
        }

        var dest = MeshStore.Publish(ResolveStore(store), json);

        Console.WriteLine($"  {Gold("✓ published to the mesh")}  {pkg!.Record.Proposal.Summary}");
        Console.WriteLine($"  {Dim($"sealed {Fp(pkg.SealSigner)} · {pkg.Files.Count} file(s)")}");
        Console.WriteLine($"  {Dim($"→ {dest}")}");
        Console.WriteLine($"  {Dim("peers pull with:  ashlar pkg pull --from " + Path.GetDirectoryName(dest))}");
        return 0;
    }

    // ─────────────────────────── share (export + publish, one verb) ───────────────────────────

    private static Command BuildShare()
    {
        var idOpt = new Option<string>("--id", "The admitted proposal to share.") { IsRequired = true };
        var storeOpt = StoreOption();
        var pathOpt = PathOption();
        var cmd = new Command("share", "Seal an admitted extension and place it in the mesh store, in one step.") { idOpt, storeOpt, pathOpt };
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await ShareAsync(
                ctx.ParseResult.GetValueForOption(idOpt)!,
                ctx.ParseResult.GetValueForOption(storeOpt),
                ctx.ParseResult.GetValueForOption(pathOpt)!);
        });
        return cmd;
    }

    private static async Task<int> ShareAsync(string id, DirectoryInfo? store, DirectoryInfo directory)
    {
        if (!File.Exists(Path.Combine(directory.FullName, "ashlar.yaml")))
        {
            Console.Error.WriteLine($"not an ashlar project: no ashlar.yaml in {directory.FullName}");
            return 1;
        }

        try
        {
            var sealer = OperatorKey.TryLoad();
            if (sealer is null)
            {
                Console.Error.WriteLine("sharing requires an operator key — the seal is a signature.");
                Console.Error.WriteLine("create one with:  ashlar keys init");
                return 1;
            }

            var (code, gathered, gatheredFiles) = await GatherAsync(id, directory);
            if (code != 0)
            {
                return code;
            }
            var (record, files) = (gathered!, gatheredFiles!);

            var json = ExtensionPackaging.Pack(record, files, sealer);
            // Same refusal shape as `pkg publish`: a package that does not verify is a 65, not an
            // operational error — share must hold every property export + publish had separately.
            if (!ExtensionPackaging.TryOpen(json, out _, out var reason))
            {
                Console.Error.WriteLine(reason);
                return 65;
            }
            var dest = MeshStore.Publish(ResolveStore(store), json);

            Console.WriteLine($"  {Gold("✓ shared to the mesh")}  {record.Proposal.Summary}");
            Console.WriteLine($"  {Dim($"{files.Count} file(s) · admitted by {record.Actor} · verdict {Fp(record.Signer)} · seal {Fp(sealer.PublicKeyBase64)}")}");
            Console.WriteLine($"  {Dim($"→ {dest}")}");
            Console.WriteLine($"  {Dim("peers pull with:  ashlar pkg pull --from " + Path.GetDirectoryName(dest))}");
            return 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    // ─────────────────────────── pull (from a peer) ───────────────────────────

    private static Command BuildPull()
    {
        // A string, not a DirectoryInfo: an `--from http://…` used to be coerced into a DirectoryInfo,
        // mangling the URL into a nonsense local path and then reporting "no such peer store: <mangled>".
        // Taking the raw token lets PullAsync catch the URL and refuse it legibly.
        var fromOpt = new Option<string>("--from", "A peer's mesh store DIRECTORY — a path, or a file:// URL naming one.") { IsRequired = true };
        var pathOpt = PathOption();
        var cmd = new Command("pull", "Pull certified packages from a peer and run each through THIS project's gate.") { fromOpt, pathOpt };
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await PullAsync(
                ctx.ParseResult.GetValueForOption(fromOpt)!,
                ctx.ParseResult.GetValueForOption(pathOpt)!);
        });
        return cmd;
    }

    // A URI scheme is a letter followed by letters, digits, '+', '-' or '.' (RFC 3986). Anything else
    // ahead of the first colon — a separator out of \?C:store, a space, a path segment — means the
    // colon belongs to a path and no scheme was written.
    private static bool LooksLikeUriScheme(string candidate)
    {
        if (candidate.Length == 0 || !char.IsAsciiLetter(candidate[0]))
        {
            return false;
        }
        foreach (var c in candidate)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '+' && c != '-' && c != '.')
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Resolves the raw <c>--from</c> token to the local directory it names, or refuses it with the
    /// operator-facing reason already composed.
    /// </summary>
    private static bool TryResolveStorePath(string fromPath, out string storePath, out string reason)
    {
        storePath = fromPath;
        reason = string.Empty;

        // An empty token reached `new DirectoryInfo("")`, which throws: the operator got a stack
        // trace where a refusal belongs.
        if (string.IsNullOrWhiteSpace(fromPath))
        {
            reason = "pull --from takes a directory and was given an empty one. Point --from at a "
                   + "local mesh store directory (e.g. the folder `ashlar pkg publish` wrote to).";
            return false;
        }

        // Whether a scheme was written is decided on the RAW TOKEN rather than by asking Uri, because
        // Uri's verdict is not the same on every host: on Unix an ordinary rooted path ("/srv/mesh")
        // parses as an absolute file: URI, so resolving every IsFile token through Uri.LocalPath would
        // quietly rewrite plain directories — a store named "50%20off" comes back percent-decoded and
        // one named "v1#2" comes back truncated at the fragment, both then reported as missing. That
        // is a live store silently lost, strictly worse than the bug it would be fixing. A path stays
        // a path unless a scheme was written; a single letter before the colon is a DOS drive
        // (C:store), never a scheme.
        var colon = fromPath.IndexOf(':');
        if (colon < 2 || !LooksLikeUriScheme(fromPath[..colon]))
        {
            return true;
        }
        var scheme = fromPath[..colon];

        // A file: URL names a directory, so it is the one URL shape this verb can honour — and the
        // shape an operator gets for free from a sync client or a file manager's "copy location".
        // Falling through left them holding `new DirectoryInfo("file:///srv/mesh")` and being told
        // their own store did not exist.
        if (string.Equals(scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(fromPath, UriKind.Absolute, out var fileUrl)
                && fileUrl.IsFile
                && !string.IsNullOrWhiteSpace(fileUrl.LocalPath))
            {
                storePath = fileUrl.LocalPath;
                return true;
            }
            reason = $"pull --from takes a directory, and '{fromPath}' is not a file URL that names one. "
                   + "Point --from at a local mesh store directory (e.g. the folder `ashlar pkg publish` wrote to).";
            return false;
        }

        // `pull --from` moves packages off a peer's mesh store, which is a local/synced DIRECTORY —
        // not an HTTP endpoint. HTTP pull is the daemon's job, driven by ASHLAR_MESH_PEERS.
        if (string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            reason = $"pull --from takes a directory, not a URL ('{fromPath}'). HTTP pull is the daemon's job — "
                   + "set ASHLAR_MESH_PEERS and let the background agent fetch. For a one-shot, point --from at a "
                   + "local mesh store directory (e.g. the folder `ashlar pkg publish` wrote to).";
            return false;
        }

        // ftp:, ssh:, s3: — nothing stands behind any of them here. Naming the scheme back beats
        // coercing the token into a nonsense DirectoryInfo and reporting it as a peer store that does
        // not exist, which reads to the operator as their own store having vanished.
        reason = $"pull --from takes a directory, not a '{scheme}:' URL ('{fromPath}'). There is no {scheme} "
               + "transport in this verb; point --from at a local mesh store directory (e.g. the folder "
               + "`ashlar pkg publish` wrote to).";
        return false;
    }

    private static async Task<int> PullAsync(string fromPath, DirectoryInfo directory)
    {
        // Refuse a URL this verb cannot serve rather than coercing it into a mangled path that then
        // "does not exist" — and resolve the one URL shape that does name a directory, file://,
        // instead of mangling that too.
        if (!TryResolveStorePath(fromPath, out var storePath, out var storeReason))
        {
            Console.Error.WriteLine(storeReason);
            return 1;
        }

        var from = new DirectoryInfo(storePath);
        if (!File.Exists(Path.Combine(directory.FullName, "ashlar.yaml"))
            || !File.Exists(Path.Combine(directory.FullName, "ashlar.policy.yaml")))
        {
            Console.Error.WriteLine($"not an ashlar project: {directory.FullName}");
            return 1;
        }
        if (!from.Exists)
        {
            Console.Error.WriteLine($"no such peer store: {from.FullName}");
            return 1;
        }

        // A mesh store is a plain directory that people sync, so it collects things that are not
        // packages. macOS writes AppleDouble sidecars (._name.ashpkg) beside every file on a
        // non-HFS share; they are resource forks, never packages, and they never parse — so
        // without this every pull from a Mac-touched share prints a screen of `× REFUSED` that
        // buries the sealer fingerprints below, which are the part worth reading.
        //
        // DELIBERATELY NOT filtering on mtime. Skipping recently-written files to avoid catching
        // an scp or Syncthing transfer mid-flight sounds right and is not: it cannot distinguish
        // "still arriving" from "just written", so a synchronous publish-then-pull — a scripted
        // fleet, or e2e-loop's own co-production scenario — silently defers a good package and the
        // admission never happens. Measured: a 3-second window failed three e2e scenarios. An
        // in-flight file is already handled safely, if noisily, by being refused as unparseable
        // and succeeding on the next pull; a deferred good package is a silent drop, which is
        // strictly worse.
        var packages = Directory.EnumerateFiles(from.FullName, "*.ashpkg")
            .Where(candidate => !Path.GetFileName(candidate).StartsWith('.'))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        if (packages.Count == 0)
        {
            Console.WriteLine($"  {Dim($"the peer store is empty — nothing to pull from {from.FullName}")}");
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine($"  {Dim($"pulling {packages.Count} package(s) from {from.FullName} — each faces YOUR gate")}");
        Console.WriteLine();

        int admitted = 0, held = 0, refused = 0, skipped = 0, warned = 0;
        foreach (var path in packages)
        {
            // A file this pass cannot read refuses its own ROW; the pass continues. A mesh store is
            // a plain synced directory, so anyone who can write to the share can drop one file into
            // it — and a sync client leaves half-arrived files and dangling links there without
            // anyone's help. Ending the pass on one of them lets a single file deny every legitimate
            // package behind it, which is the denial the size guard exists to prevent, moved up one
            // level. The row counts as refused, so the pass still exits 65 and the summary names it:
            // a refusal must never quietly become a skip.
            string packageJson;
            try
            {
                if (!TryCheckPackageSize(new FileInfo(path), out var sizeReason))
                {
                    refused++;
                    // No sealer fingerprint on this line: nothing was opened, so there is no verified
                    // signer to name — the same reason the parse refusals below leave it blank.
                    Console.WriteLine($"  {Bad("× REFUSED")}  {Path.GetFileName(path)} {Dim(" · " + sizeReason)}");
                    continue;
                }
                packageJson = await File.ReadAllTextAsync(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                refused++;
                Console.WriteLine($"  {Bad("× REFUSED")}  {Path.GetFileName(path)} {Dim($" · unreadable: {ex.Message}")}");
                continue;
            }

            var result = await PackageImport.SubmitAsync(directory.FullName, packageJson);
            var summary = result.Package?.Record.Proposal.Summary ?? Path.GetFileName(path);

            // WHO SEALED THIS. The summary beside it is attacker-chosen text — it is whatever the
            // sender typed — so a gold checkmark next to a friendly sentence is not evidence of
            // anything. The fingerprint is the only part of the line the sender cannot choose.
            //
            // A node cannot yet DISTINGUISH signers (there is no trust root; `ashlar keys trust`
            // is Phase 3), so the operator making the call is the control. Asking them to decide
            // while withholding the identity is the gap this closes.
            //
            // Deliberately not printed when the package did not parse: there is no verified
            // sealer to name, and "(unsigned)" there would read as a claim about a package that
            // was never opened.
            var sealedBy = result.Package?.SealSigner is { } signer
                ? $" · sealed by {Fp(signer)}"
                : string.Empty;

            switch (result.Outcome)
            {
                case PackageAdmission.Admitted:
                    admitted++;
                    Console.WriteLine($"  {Gold("✓ ADMITTED")}  {summary} {Dim($"{sealedBy} · {result.AppliedPaths.Count} file(s)")}");
                    if (result.Warning is not null)
                    {
                        warned++;
                        Console.WriteLine($"    {Bad("! " + result.Warning)}");
                    }
                    break;
                case PackageAdmission.Held:
                    held++;
                    Console.WriteLine($"  {Clay("! HELD")}     {summary} {Dim($"{sealedBy} · review with `ashlar gates`")}");
                    break;
                case PackageAdmission.AlreadyImported:
                    skipped++;
                    Console.WriteLine($"  {Dim("− already have  " + summary + sealedBy)}");
                    break;
                case PackageAdmission.Rejected:
                    refused++;
                    Console.WriteLine($"  {Bad("× REJECTED")} {summary} {Dim($"{sealedBy} · {result.Message}")}");
                    break;
                default:
                    refused++;
                    Console.WriteLine($"  {Bad("× REFUSED")}  {summary} {Dim($"{sealedBy} · {result.Message}")}");
                    break;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"  {Dim($"pulled {packages.Count} · admitted {admitted} · held {held} · already-have {skipped} · refused/rejected {refused}")}");
        // A partial apply (admitted but not fully written) is a non-zero exit so a script notices.
        if (warned > 0)
        {
            return 1;
        }
        // A refusal is never masked by another package in the same pull succeeding.
        //
        // The mesh store is a plain directory, so anyone who can write to the share can plant a
        // package; refusing it is only half the job, because the operator still has to find out.
        // A fleet pulling on a timer reaches a steady state where every legitimate package is
        // already-had, so a rule that treats "something succeeded" as success is true on every
        // run — and a planted forgery would report `refused/rejected 1` in the body while exiting
        // 0 forever. `pkg pull … && echo ok` must not print ok when this gate refused something.
        return refused > 0 ? 65 : 0;
    }

    private static string Fp(string? publicKeyBase64) =>
        publicKeyBase64 is null ? "(unsigned)" : OperatorKey.Fingerprint(Convert.FromBase64String(publicKeyBase64));

    // Same colour discipline as the other verbs.
    private static readonly bool Color =
        Environment.GetEnvironmentVariable("NO_COLOR") is null && !Console.IsOutputRedirected;
    private static string Paint(string ansi, string t) => Color ? $"\x1b[{ansi}m{t}\x1b[0m" : t;
    private static string Ok(string t) => Paint("32", t);
    private static string Bad(string t) => Paint("31", t);
    private static string Gold(string t) => Paint("33", t);
    private static string Clay(string t) => Paint("38;5;173", t);
    private static string Dim(string t) => Paint("90", t);
}
