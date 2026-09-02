using System.CommandLine;
using System.CommandLine.Invocation;
using Ashlar.Manifest.Ledger;
using Ashlar.Manifest.Signing;

namespace Ashlar.CLI.Commands;

/// <summary>
/// <c>ashlar ledger</c> — inspect and, where the operator decides to, repair a project's signed
/// instance ledger.
///
/// <para>Why this verb exists: the ledger's refusals name their fix, and two of them named a fix
/// that only the KERNEL could execute. <c>InstanceLedgerAnchor</c> refuses a chain shorter than its
/// anchor, and a ledger whose entries are gone from under a live anchor, because both are what
/// truncation looks like and neither may be cleared by an ordinary <c>ashlar verify</c> — otherwise
/// a fresh valid-looking head could always be written over a history someone deleted. The
/// deliberate acceptance of that loss lives in <see cref="InstanceLedger.ReanchorAsync"/>, and until
/// this file existed the refusal could only name it as a C# identifier. An operator standing at a
/// terminal cannot run a method. <c>ashlar ledger reanchor</c> is that method as a command, so every
/// ledger refusal now names something the person reading it can actually type.</para>
///
/// <para><c>status</c> is the read: it runs the same fail-closed verification the courses run and
/// prints either the shape of the history or, verbatim, the refusal a <c>verify</c> would hit — so
/// the operator can see the disagreement before deciding what to do about it.</para>
/// </summary>
public sealed class LedgerCommand : Command
{
    /// <summary>Creates a new LedgerCommand instance.</summary>
    public LedgerCommand() : base("ledger", "Inspect this project's signed instance ledger, and re-anchor it when the operator accepts a loss.")
    {
        AddCommand(BuildStatus());
        AddCommand(BuildReanchor());
    }

    private const int ExitOk = 0;
    private const int ExitUsage = 1;
    private const int ExitRefused = 65;

    private static Option<DirectoryInfo> PathOption() => new(
        name: "--path",
        description: "Project directory containing .ashlar/ (defaults to current).",
        getDefaultValue: () => new DirectoryInfo(Environment.CurrentDirectory));

    private static Option<string?> KeyDirOption() => new(
        name: "--key-dir",
        description: "Key directory (defaults to $ASHLAR_KEY_DIR, else ~/.ashlar/keys).");

    // -- status --------------------------------------------------------------

    private static Command BuildStatus()
    {
        var pathOpt = PathOption();
        var cmd = new Command("status", "Verify the ledger chain and its head anchor, and say exactly what is wrong when it refuses.") { pathOpt };
        cmd.SetHandler((InvocationContext ctx) =>
        {
            ctx.ExitCode = Status(ctx.ParseResult.GetValueForOption(pathOpt)!);
        });
        return cmd;
    }

    /// <summary>Testable core of <c>ashlar ledger status</c>.</summary>
    internal static int Status(DirectoryInfo directory, TextWriter? stdout = null, TextWriter? stderr = null)
    {
        var outw = stdout ?? Console.Out;
        var errw = stderr ?? Console.Error;
        var stateRoot = Path.Combine(directory.FullName, ".ashlar");
        var ledger = new InstanceLedger(stateRoot);

        if (!Directory.Exists(stateRoot) || (!ledger.HasHeadAnchor && !HasEntries(stateRoot)))
        {
            outw.WriteLine($"  {Dim($"no ledger in {directory.FullName} - this project has never been certified.")}");
            outw.WriteLine($"  {Dim("certify it with:  ashlar keys init && ashlar verify")}");
            return ExitOk;
        }

        try
        {
            var chain = ledger.VerifyChain();
            outw.WriteLine($"  {Gold("OK ledger intact")}  {Dim($"{chain.Count} signed entr{(chain.Count == 1 ? "y" : "ies")} - anchor agrees with the head")}");
            if (chain.Head is { } head)
            {
                outw.WriteLine($"  {Dim($"head #{head.Seq} - {head.At:u} - subject {Short(head.Subject)}")}");
            }
            return ExitOk;
        }
        catch (InvalidOperationException ex)
        {
            // The kernel's refusal, verbatim - it already names the fix, and paraphrasing a
            // fail-closed message is how a fix that works turns into one that does not.
            errw.WriteLine("  ledger refused");
            errw.WriteLine($"  {ex.Message}");
            return ExitRefused;
        }
    }

    // -- reanchor ------------------------------------------------------------

    private static Command BuildReanchor()
    {
        var pathOpt = PathOption();
        var keyDirOpt = KeyDirOption();
        var yesOpt = new Option<bool>(
            name: "--yes",
            description: "Confirm that the history is meant to be this long. Required: a re-anchor ACCEPTS whatever is missing.");
        var cmd = new Command(
            "reanchor",
            "Re-pin the head anchor over the surviving entries - the deliberate, signed acceptance of a shortened history.")
        {
            pathOpt, keyDirOpt, yesOpt,
        };
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await ReanchorAsync(
                ctx.ParseResult.GetValueForOption(pathOpt)!,
                ctx.ParseResult.GetValueForOption(keyDirOpt),
                ctx.ParseResult.GetValueForOption(yesOpt),
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        return cmd;
    }

    /// <summary>Testable core of <c>ashlar ledger reanchor</c>.</summary>
    internal static async Task<int> ReanchorAsync(
        DirectoryInfo directory,
        string? keyDir,
        bool yes,
        CancellationToken ct,
        TextWriter? stdout = null,
        TextWriter? stderr = null)
    {
        var outw = stdout ?? Console.Out;
        var errw = stderr ?? Console.Error;
        var stateRoot = Path.Combine(directory.FullName, ".ashlar");
        var ledger = new InstanceLedger(stateRoot);

        if (!Directory.Exists(stateRoot) || (!ledger.HasHeadAnchor && !HasEntries(stateRoot)))
        {
            errw.WriteLine($"there is no ledger under {stateRoot} - nothing to re-anchor.");
            errw.WriteLine("re-anchoring accepts a history that is SHORTER than its anchor said; with no history and no");
            errw.WriteLine("anchor there is no such disagreement. certify the project instead:  ashlar keys init && ashlar verify");
            return ExitUsage;
        }

        SigningIdentity? signer;
        try
        {
            signer = OperatorKey.TryLoad(keyDir);
        }
        catch (InvalidOperationException ex)
        {
            // A corrupt operator key fails closed - the same rule `ashlar verify` follows.
            errw.WriteLine(ex.Message);
            return ExitUsage;
        }

        if (signer is null)
        {
            errw.WriteLine("refusing to re-anchor: there is no operator key on this machine, and a re-anchor is a SIGNED act -");
            errw.WriteLine("that is what makes it a decision someone took rather than something that happened.");
            errw.WriteLine("fix: run `ashlar keys init` on the machine that owns this project, then run this command again.");
            errw.WriteLine($"if the key lives elsewhere, point ASHLAR_KEY_DIR at it, or pass --key-dir <dir> (looked in {OperatorKey.ResolveKeyDir()}).");
            return ExitUsage;
        }

        // What is being accepted, shown BEFORE it is accepted. The kernel's own refusal is the
        // most accurate description of the disagreement, so it is printed rather than summarised.
        string? disagreement = null;
        try
        {
            ledger.VerifyChain();
        }
        catch (InvalidOperationException ex)
        {
            disagreement = ex.Message;
        }

        if (disagreement is null)
        {
            outw.WriteLine($"  {Dim("the ledger already verifies - its anchor agrees with its head. nothing to re-anchor.")}");
            outw.WriteLine($"  {Dim("re-anchoring an intact ledger would rewrite the anchor for no reason; refusing to do it silently.")}");
            return ExitOk;
        }

        if (!yes)
        {
            errw.WriteLine("refusing to re-anchor without --yes. this is what you would be accepting:");
            errw.WriteLine();
            errw.WriteLine($"  {disagreement}");
            errw.WriteLine();
            errw.WriteLine("a re-anchor does NOT recover anything. it re-verifies every entry that is still here and then");
            errw.WriteLine("declares that length to be the intended one, signed with your operator key. whatever is missing");
            errw.WriteLine("stays missing, and the fact that it went missing stops being detectable.");
            errw.WriteLine();
            errw.WriteLine("if the entries can be restored, restore them first - that is the repair that keeps the history:");
            errw.WriteLine("  restore .ashlar/ledger and .ashlar/ledger.head.json from backup, then:  ashlar ledger status");
            errw.WriteLine("if the loss is genuinely accepted, re-run with:");
            errw.WriteLine($"  ashlar ledger reanchor --path \"{directory.FullName}\" --yes");
            return ExitUsage;
        }

        try
        {
            var chain = await ledger.ReanchorAsync(signer, DateTimeOffset.UtcNow, ct).ConfigureAwait(false);
            outw.WriteLine($"  {Gold("OK ledger re-anchored")}  {Dim($"{chain.Count} surviving entr{(chain.Count == 1 ? "y" : "ies")} - signed {signer.Fingerprint}")}");
            outw.WriteLine($"  {Dim("what was accepted:")}");
            outw.WriteLine($"  {Dim(disagreement)}");
            outw.WriteLine($"  {Dim("the anchor now pins this history. re-certify the documents over it with:  ashlar verify")}");
            return ExitOk;
        }
        catch (InvalidOperationException ex)
        {
            // ReanchorAsync verifies the whole chain first and refuses an empty ledger. Neither is
            // something --yes may override: a re-anchor accepts a SHORTER history, never a forged
            // one, and there is no honest anchor to write over nothing.
            errw.WriteLine("  re-anchor refused");
            errw.WriteLine($"  {ex.Message}");
            return ExitRefused;
        }
    }

    private static bool HasEntries(string stateRoot)
    {
        var ledgerDir = Path.Combine(stateRoot, "ledger");
        return Directory.Exists(ledgerDir)
            && Directory.EnumerateFiles(ledgerDir, "*.json")
                .Any(f => !f.EndsWith(".json.tmp", StringComparison.Ordinal));
    }

    private static string Short(string? subject) =>
        string.IsNullOrEmpty(subject) ? "(none)" : (subject.Length <= 16 ? subject : subject[..16] + "...");

    // -- rendering (SPEC-009 vocabulary: gold is the verdict, nothing else) ---

    private static readonly bool Color =
        Environment.GetEnvironmentVariable("NO_COLOR") is null && !Console.IsOutputRedirected;

    private static string Paint(string ansi, string text) => Color ? $"\x1b[{ansi}m{text}\x1b[0m" : text;
    private static string Gold(string t) => Paint("33", t);
    private static string Dim(string t) => Paint("90", t);
}
