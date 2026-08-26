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
        if (!ExtensionPackaging.TryOpen(await File.ReadAllTextAsync(file.FullName), out var peek, out var peekReason))
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

        var result = await PackageImport.SubmitAsync(directory.FullName, await File.ReadAllTextAsync(file.FullName));
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
        if (!ExtensionPackaging.TryOpen(await File.ReadAllTextAsync(file.FullName), out var pkg, out var reason))
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
        var fromOpt = new Option<DirectoryInfo>("--from", "A peer's mesh store to pull certified packages from.") { IsRequired = true };
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

    private static async Task<int> PullAsync(DirectoryInfo from, DirectoryInfo directory)
    {
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

        var packages = Directory.EnumerateFiles(from.FullName, "*.ashpkg").OrderBy(p => p, StringComparer.Ordinal).ToList();
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
            var result = await PackageImport.SubmitAsync(directory.FullName, await File.ReadAllTextAsync(path));
            var summary = result.Package?.Record.Proposal.Summary ?? Path.GetFileName(path);
            switch (result.Outcome)
            {
                case PackageAdmission.Admitted:
                    admitted++;
                    Console.WriteLine($"  {Gold("✓ ADMITTED")}  {summary} {Dim($"· {result.AppliedPaths.Count} file(s)")}");
                    if (result.Warning is not null)
                    {
                        warned++;
                        Console.WriteLine($"    {Bad("! " + result.Warning)}");
                    }
                    break;
                case PackageAdmission.Held:
                    held++;
                    Console.WriteLine($"  {Clay("! HELD")}     {summary} {Dim("· review with `ashlar gates`")}");
                    break;
                case PackageAdmission.AlreadyImported:
                    skipped++;
                    Console.WriteLine($"  {Dim("− already have  " + summary)}");
                    break;
                case PackageAdmission.Rejected:
                    refused++;
                    Console.WriteLine($"  {Bad("× REJECTED")} {summary} {Dim($"· {result.Message}")}");
                    break;
                default:
                    refused++;
                    Console.WriteLine($"  {Bad("× REFUSED")}  {summary} {Dim($"· {result.Message}")}");
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
        // A pull where nothing new was accepted AND nothing was already-had is worth a non-zero exit
        // so a script notices a peer sending only things this gate refuses.
        return admitted + held + skipped > 0 ? 0 : 65;
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
