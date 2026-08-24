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
/// </summary>
public sealed class PkgCommand : Command
{
    /// <summary>Creates a new PkgCommand instance.</summary>
    public PkgCommand() : base("pkg", "Export, inspect, and import certified extension packages (.ashpkg).")
    {
        AddCommand(BuildExport());
        AddCommand(BuildImport());
        AddCommand(BuildShow());
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

            var store = new GateStore(Path.Combine(directory.FullName, ".ashlar"));
            var record = await store.GetAsync(id);
            if (record is null)
            {
                Console.Error.WriteLine($"no proposal '{id}' in the store.");
                return 1;
            }

            // The files are the parked writes the gate admitted, straight from the forge queue.
            var forge = AshlarProjectMediation.ProjectStore(directory.FullName);
            var files = new List<PackageFile>();
            foreach (var forgeId in record.Proposal.ForgeProposalIds)
            {
                var proposal = forge.Find(forgeId);
                if (proposal is null)
                {
                    Console.Error.WriteLine($"forge proposal '{forgeId}' referenced by '{id}' is missing from the forge store — cannot package an incomplete extension.");
                    return 1;
                }
                files.Add(new PackageFile { Path = proposal.TargetPath, Content = proposal.NewContent });
            }

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

        // Intrinsic verification: both signatures check against keys the package carries.
        // Failing ANY check is refusal — a package that half-verifies is treated as forged.
        if (!ExtensionPackaging.TryOpen(await File.ReadAllTextAsync(file.FullName), out var pkg, out var reason))
        {
            Console.Error.WriteLine(reason);
            return 65;
        }

        if (!PolicyLoader.TryLoad(await File.ReadAllTextAsync(policyPath), out var policy, out var policyReason))
        {
            Console.Error.WriteLine(policyReason);
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine($"  {pkg!.Record.Proposal.Summary}");
        Console.WriteLine($"  {Dim($"origin verdict {Fp(pkg.Record.Signer)} · seal {Fp(pkg.SealSigner)} · {pkg.Files.Count} file(s)")}");
        foreach (var course in pkg.Record.Proposal.Courses)
        {
            var glyph = course.Passed ? Ok("✓") : Bad("×");
            Console.WriteLine($"  {glyph} {course.Name,-12} {Dim($"{course.Detail} (evidence from origin)")}");
        }
        Console.WriteLine();

        try
        {
            // Park the files as LOCAL forge proposals — nothing touches the project tree until
            // THIS gate admits. Propose → hold → apply holds for imports exactly as for local
            // cycles: sealed seals against remote code by construction.
            var forge = AshlarProjectMediation.ProjectStore(directory.FullName);
            var localForgeIds = new List<string>();
            foreach (var pf in pkg.Files)
            {
                var parked = forge.Add(new ChangeProposal
                {
                    Id = "pkg-" + Guid.NewGuid().ToString("N")[..12],
                    TargetPath = pf.Path,
                    NewContent = pf.Content,
                    Summary = $"imported: {pkg.Record.Proposal.Summary}",
                    Reason = $"package sealed by {Fp(pkg.SealSigner)}",
                    CreatedAt = DateTimeOffset.UtcNow,
                });
                localForgeIds.Add(parked.Id);
            }

            var proposal = pkg.Record.Proposal with { ForgeProposalIds = localForgeIds };
            var store = new GateStore(Path.Combine(directory.FullName, ".ashlar"), OperatorKey.TryLoad());
            var record = await store.ProposeAsync(policy!, proposal, DateTimeOffset.UtcNow);

            switch (record.State)
            {
                case ProposalState.Admitted:
                    var applied = ForgeApplier.ApplyAll(forge, localForgeIds, directory.FullName, "gate");
                    Console.WriteLine($"  {Gold($"✓ ADMITTED — {record.Reason}")}");
                    foreach (var path in applied)
                    {
                        Console.WriteLine($"  {Ok("✓ applied")}  {path}");
                    }
                    return 0;
                case ProposalState.Held:
                    Console.WriteLine($"  {Clay($"! HELD — {record.Reason}")}");
                    Console.WriteLine($"  {Dim("nothing is on disk. review:  ashlar gates --show " + record.Proposal.Id)}");
                    return 0;
                default:
                    ForgeApplier.RejectAll(forge, localForgeIds, "gate", record.Reason);
                    Console.WriteLine($"  {Bad($"× REJECTED — {record.Reason}")}");
                    Console.WriteLine($"  {Dim("disk untouched")}");
                    return 65;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // Append-once id collisions, corrupt local key, forge/apply refusals: the kernel's
            // wording is the contract; pass it through.
            Console.Error.WriteLine(ex.Message);
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
