using System.CommandLine;
using System.CommandLine.Invocation;
using Ashlar.Manifest;
using Ashlar.Manifest.Admission;

namespace Ashlar.CLI.Commands;

/// <summary>
/// <c>ashlar gates</c> — the human half of admission: list what is held, inspect one
/// proposal, seat the stone or refuse with a reason.
///
/// <para>Thin over the kernel: every rule (who may transition what, refusals require
/// reasons, immutable history) lives in <see cref="GateStore"/> and is pinned by kernel
/// tests; this class reads the store under <c>.ashlar/</c> in the project directory and
/// renders in the fixed vocabulary. Gold appears only when a stone is seated.</para>
/// </summary>
public sealed class GatesCommand : Command
{
    /// <summary>Creates a new GatesCommand instance.</summary>
    public GatesCommand() : base("gates", "List held proposals; seat the stone or refuse, with a reason.")
    {
        var pathOpt = new Option<DirectoryInfo>(
            name: "--path",
            description: "Project directory (defaults to current).",
            getDefaultValue: () => new DirectoryInfo(Environment.CurrentDirectory));
        var showOpt = new Option<string?>("--show", "Show one proposal in full.");
        var admitOpt = new Option<string?>("--admit", "Seat the stone on a held proposal.");
        var refuseOpt = new Option<string?>("--refuse", "Refuse a held proposal.");
        var reasonOpt = new Option<string?>("--reason", "Why (required with --refuse; recorded and fed back to the proposer).");
        var actorOpt = new Option<string>(
            name: "--as",
            description: "Who is deciding. Recorded in the ledger.",
            getDefaultValue: () => Environment.UserName);

        AddGlobalOption(pathOpt); AddOption(showOpt); AddOption(admitOpt);
        AddOption(refuseOpt); AddOption(reasonOpt); AddOption(actorOpt);

        // Hidden: the runtime's entry. A proposal arrives as JSON, the policy decides its
        // fate (AdmissionGate rule order), the outcome is recorded. The SelfExtendRunner
        // calls this path when it wires in; until then it is how tests and demos feed the
        // queue. Hidden because users never propose — applications do.
        var proposeFileOpt = new Option<FileInfo>("--file", "Proposal JSON.") { IsRequired = true };
        var proposeCmd = new Command("propose", "Submit an evaluated proposal (runtime entry).")
        {
            proposeFileOpt,
        };
        proposeCmd.IsHidden = true;
        proposeCmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await ProposeAsync(
                ctx.ParseResult.GetValueForOption(pathOpt) ?? new DirectoryInfo(Environment.CurrentDirectory),
                ctx.ParseResult.GetValueForOption(proposeFileOpt)!);
        });
        AddCommand(proposeCmd);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await ExecuteAsync(
                ctx.ParseResult.GetValueForOption(pathOpt)!,
                ctx.ParseResult.GetValueForOption(showOpt),
                ctx.ParseResult.GetValueForOption(admitOpt),
                ctx.ParseResult.GetValueForOption(refuseOpt),
                ctx.ParseResult.GetValueForOption(reasonOpt),
                ctx.ParseResult.GetValueForOption(actorOpt)!);
        });
    }

    private static async Task<int> ExecuteAsync(
        DirectoryInfo directory, string? show, string? admit, string? refuse, string? reason, string actor)
    {
        if (!File.Exists(Path.Combine(directory.FullName, "ashlar.yaml")))
        {
            Console.Error.WriteLine($"not an ashlar project: no ashlar.yaml in {directory.FullName}");
            return 1;
        }
        var store = new GateStore(Path.Combine(directory.FullName, ".ashlar"));

        try
        {
            if (admit is not null)
            {
                var decided = await store.DecideAsync(admit, admit: true, actor, reason ?? "seated after review", DateTimeOffset.UtcNow);
                Console.WriteLine($"  {Gold("✓ seated")}  {decided.Proposal.Summary}");
                Console.WriteLine($"  {Dim($"admitted by {decided.Actor} · recorded")}");

                // M1 apply: seating the stone is what puts the held writes on disk. The
                // decision is already recorded above; an apply failure is reported loudly
                // and the operator re-runs the apply — it must never look like the writes
                // landed when they did not.
                if (decided.Proposal.ForgeProposalIds.Count > 0)
                {
                    try
                    {
                        var forge = Ashlar.BackgroundAgents.HostRunners.AshlarProjectMediation.ProjectStore(directory.FullName);
                        var applied = Ashlar.BackgroundAgents.HostRunners.ForgeApplier.ApplyAll(
                            forge, decided.Proposal.ForgeProposalIds, directory.FullName, actor);
                        foreach (var path in applied)
                        {
                            Console.WriteLine($"  {Ok("✓ applied")}  {path}");
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.Error.WriteLine($"  APPLY FAILED — the admission is recorded but the writes are NOT on disk: {ex.Message}");
                        return 1;
                    }
                }
                return 0;
            }
            if (refuse is not null)
            {
                var decided = await store.DecideAsync(refuse, admit: false, actor, reason ?? string.Empty, DateTimeOffset.UtcNow);
                Console.WriteLine($"  {Clay("! refused")}  {decided.Proposal.Summary}");
                Console.WriteLine($"  {Dim($"reason recorded and fed back: {decided.Reason}")}");

                // The refusal rejects the parked writes too — no orphaned pending work.
                if (decided.Proposal.ForgeProposalIds.Count > 0)
                {
                    var forge = Ashlar.BackgroundAgents.HostRunners.AshlarProjectMediation.ProjectStore(directory.FullName);
                    Ashlar.BackgroundAgents.HostRunners.ForgeApplier.RejectAll(
                        forge, decided.Proposal.ForgeProposalIds, actor, decided.Reason);
                    Console.WriteLine($"  {Dim($"{decided.Proposal.ForgeProposalIds.Count} parked write(s) rejected — disk untouched")}");
                }
                return 0;
            }
            if (show is not null)
            {
                return await ShowAsync(store, show);
            }
            return await ListAsync(store);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // The kernel's rules speaking (only Held can be decided; refusals need reasons;
            // unknown ids). Their wording is the contract; pass it through.
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task<int> ProposeAsync(DirectoryInfo directory, FileInfo file)
    {
        var policyPath = Path.Combine(directory.FullName, "ashlar.policy.yaml");
        if (!File.Exists(policyPath))
        {
            Console.Error.WriteLine($"not an ashlar project: no ashlar.policy.yaml in {directory.FullName}");
            return 1;
        }
        if (!PolicyLoader.TryLoad(await File.ReadAllTextAsync(policyPath), out var policy, out var reason))
        {
            Console.Error.WriteLine(reason);
            return 1;
        }

        ExtensionProposal? proposal;
        try
        {
            proposal = System.Text.Json.JsonSerializer.Deserialize<ExtensionProposal>(
                await File.ReadAllTextAsync(file.FullName),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (System.Text.Json.JsonException ex)
        {
            Console.Error.WriteLine($"REJECTED: proposal could not be parsed: {ex.Message}");
            return 1;
        }
        if (proposal is null)
        {
            Console.Error.WriteLine("REJECTED: proposal file was empty.");
            return 1;
        }

        var store = new GateStore(Path.Combine(directory.FullName, ".ashlar"));

        // One call, one transaction. The count-decide-record sequence lives kernel-side
        // under the store lock (#373) so a racing process can never separate the budget
        // check from the recording — this command must not reassemble that race by calling
        // the pieces itself.
        var record = await store.ProposeAsync(policy!, proposal, DateTimeOffset.UtcNow);

        var line = record.State switch
        {
            ProposalState.Held => Clay($"! HELD — {record.Reason}"),
            ProposalState.Admitted => Gold($"✓ ADMITTED — {record.Reason}"),
            _ => Bad($"× REJECTED — {record.Reason}"),
        };
        Console.WriteLine($"  {line}");
        return record.State == ProposalState.Rejected ? 65 : 0;
    }

    private static async Task<int> ListAsync(GateStore store)
    {
        var held = await store.ListAsync(ProposalState.Held);
        if (held.Count == 0)
        {
            Console.WriteLine($"  {Dim("nothing held — the wall is quiet")}");
            return 0;
        }

        Console.WriteLine();
        foreach (var r in held)
        {
            Console.WriteLine($"  {Clay("!")} {r.Proposal.Id,-12} {r.Proposal.Summary}");
            Console.WriteLine($"    {Dim($"by {r.Proposal.ProposedBy} · {r.Proposal.ProposedAt:HH:mm 'UTC'} · {r.Reason}")}");
        }
        Console.WriteLine();
        Console.WriteLine($"  {Dim("inspect:")} ashlar gates --show <id>   {Dim("seat:")} ashlar gates --admit <id>");
        return 0;
    }

    private static async Task<int> ShowAsync(GateStore store, string id)
    {
        var r = await store.GetAsync(id);
        if (r is null)
        {
            Console.Error.WriteLine($"no proposal '{id}' in the store.");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine($"  {r.Proposal.Summary}");
        Console.WriteLine($"  {Dim($"{r.Proposal.Id} · kind {r.Proposal.Kind} · by {r.Proposal.ProposedBy} · {r.Proposal.ProposedAt:u}")}");
        Console.WriteLine();
        foreach (var c in r.Proposal.Courses)
        {
            var glyph = c.Passed ? Ok("✓") : Bad("×");
            Console.WriteLine($"  {glyph} {c.Name,-12} {Dim(c.Detail)}");
        }
        Console.WriteLine();
        var state = r.State switch
        {
            ProposalState.Held => Clay($"! HELD — {r.Reason}"),
            ProposalState.Admitted => Gold($"✓ ADMITTED by {r.Actor}"),
            ProposalState.Refused => Dim($"REFUSED by {r.Actor}: {r.Reason}"),
            _ => Bad($"× REJECTED — {r.Reason}"),
        };
        Console.WriteLine($"  {state}");
        if (!string.IsNullOrWhiteSpace(r.Proposal.Diff))
        {
            Console.WriteLine();
            Console.WriteLine($"  {Dim("change:")}");
            foreach (var line in r.Proposal.Diff.Split('\n'))
            {
                Console.WriteLine($"    {Dim(line.TrimEnd())}");
            }
        }
        return 0;
    }

    // Same colour discipline as VerifyCommand: gold for a seated stone only.
    private static readonly bool Color =
        Environment.GetEnvironmentVariable("NO_COLOR") is null && !Console.IsOutputRedirected;
    private static string Paint(string ansi, string t) => Color ? $"\x1b[{ansi}m{t}\x1b[0m" : t;
    private static string Ok(string t) => Paint("32", t);
    private static string Bad(string t) => Paint("31", t);
    private static string Gold(string t) => Paint("33", t);
    private static string Clay(string t) => Paint("38;5;173", t);
    private static string Dim(string t) => Paint("90", t);
}
