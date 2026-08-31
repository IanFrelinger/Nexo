using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.RegularExpressions;
using Ashlar.Manifest;
using Ashlar.Manifest.Admission;

namespace Ashlar.CLI.Commands;

/// <summary>
/// <c>ashlar policy</c> — read and safely raise/lower the one policy dial an operator turns after
/// deploy: the self-extend mode. New projects ship <c>sealed</c> (nothing changes after deploy). The
/// staged auto-apply opt-in is <c>ashlar policy set self_extend proposing</c> (propose &amp; hold for
/// review) or <c>self-extending</c> (auto-admit within budget, gated by the post-apply canary) — the
/// exact command the gate's own refusal and the project scaffold already tell you to run.
///
/// <para>This is the only supported way to edit the operator-owned policy after <c>init</c>, and it is
/// deliberately narrow: it changes <b>only</b> <c>selfExtend.mode</c>, never the governance floor
/// (<c>never</c>, <c>sandbox</c>, <c>trustedSigners</c>). It edits in place — preserving every other
/// field and comment — and validates the RESULT before writing, so a flip that would produce an
/// invalid policy (e.g. <c>self-extending</c> with no <c>gatesRequired</c>) is refused with nothing
/// written. The daemon re-reads the policy each cycle, so the change takes effect without a restart.</para>
/// </summary>
public sealed class PolicyCommand : Command
{
    /// <summary>Creates a new PolicyCommand instance.</summary>
    public PolicyCommand() : base("policy", "Inspect and set the project's self-extend policy dial.")
    {
        AddCommand(BuildShow());
        AddCommand(BuildSet());
    }

    private static Option<string> PathOption() => new(
        name: "--path",
        getDefaultValue: () => ".",
        description: "Project directory containing ashlar.policy.yaml (defaults to the current directory).");

    private static Command BuildShow()
    {
        var pathOpt = PathOption();
        var cmd = new Command("show", "Print the project's self-extend posture (mode, budget, gates, trust).") { pathOpt };
        cmd.SetHandler((InvocationContext ctx) =>
            ctx.ExitCode = Show(ctx.ParseResult.GetValueForOption(pathOpt)!, Console.Out, Console.Error));
        return cmd;
    }

    private static Command BuildSet()
    {
        // `policy set self_extend <mode>` — matches the phantom command the gate refusal and the
        // scaffold already print, so that guidance now works verbatim.
        var keyArg = new Argument<string>("key", "The dial to set. Only 'self_extend' is supported.");
        var valueArg = new Argument<string>("value", "New value: sealed | proposing | self-extending.");
        var pathOpt = PathOption();
        var cmd = new Command("set", "Set a policy dial. Only `set self_extend <mode>` is supported.")
        {
            keyArg, valueArg, pathOpt
        };
        cmd.SetHandler((InvocationContext ctx) =>
            ctx.ExitCode = Set(
                ctx.ParseResult.GetValueForArgument(keyArg),
                ctx.ParseResult.GetValueForArgument(valueArg),
                ctx.ParseResult.GetValueForOption(pathOpt)!,
                Console.Out, Console.Error));
        return cmd;
    }

    /// <summary>Read-only view of the self-extend posture. Testable via injected writers.</summary>
    public static int Show(string projectDir, TextWriter stdout, TextWriter stderr)
    {
        var policyPath = Path.Combine(projectDir, "ashlar.policy.yaml");
        if (!File.Exists(policyPath))
        {
            stderr.WriteLine($"not an ashlar project (no ashlar.policy.yaml at {projectDir})");
            return 1;
        }
        if (!PolicyLoader.TryLoad(File.ReadAllText(policyPath), out var policy, out var reason))
        {
            stderr.WriteLine($"policy is invalid: {reason}");
            return 1;
        }
        var se = policy!.SelfExtend;
        stdout.WriteLine($"self-extend mode : {se.Mode}");
        stdout.WriteLine($"  budget         : {se.Budget.Extensions} per {se.Budget.Window}");
        stdout.WriteLine($"  gatesRequired  : [{string.Join(", ", se.GatesRequired)}]");
        stdout.WriteLine($"  mayAdd         : [{string.Join(", ", se.MayAdd)}]");
        stdout.WriteLine($"  trustedSigners : {se.TrustedSigners.Count}");
        stdout.WriteLine();
        stdout.WriteLine(se.Mode switch
        {
            SelfExtendMode.Sealed => "sealed: nothing changes after deploy. Raise with `ashlar policy set self_extend proposing`.",
            SelfExtendMode.Proposing => "proposing: self-extend cycles are HELD for review (`ashlar gates`). No auto-apply.",
            SelfExtendMode.SelfExtending => "self-extending: admitted cycles AUTO-APPLY within budget, gated by the post-apply canary.",
            _ => string.Empty,
        });
        return 0;
    }

    /// <summary>
    /// Sets the self-extend mode in place. Testable via injected writers. Only the <c>self_extend</c>
    /// key is accepted; the value must be a valid mode; the edited policy must still load, or nothing
    /// is written.
    /// </summary>
    public static int Set(string key, string value, string projectDir, TextWriter stdout, TextWriter stderr)
    {
        var normalizedKey = key.Replace('-', '_').ToLowerInvariant();
        if (normalizedKey is not ("self_extend" or "selfextend" or "mode"))
        {
            stderr.WriteLine($"unsupported key '{key}'. Only `self_extend` (the mode) can be set; the governance "
                + "floor (never, sandbox, trustedSigners) is not editable through this command — edit ashlar.policy.yaml directly for those.");
            return 1;
        }

        var mode = value.Trim();
        if (!SelfExtendMode.All.Contains(mode, StringComparer.Ordinal))
        {
            stderr.WriteLine($"unknown mode '{value}'; expected one of {string.Join(", ", SelfExtendMode.All)}.");
            return 1;
        }

        var policyPath = Path.Combine(projectDir, "ashlar.policy.yaml");
        if (!File.Exists(policyPath))
        {
            stderr.WriteLine($"not an ashlar project (no ashlar.policy.yaml at {projectDir})");
            return 1;
        }

        var original = File.ReadAllText(policyPath);
        // Refuse to edit a policy that does not already load — never turn a broken policy into a
        // different broken policy; the operator must fix it first.
        if (!PolicyLoader.TryLoad(original, out var current, out var currentReason))
        {
            stderr.WriteLine($"current policy is invalid: {currentReason}. Fix it before setting a mode.");
            return 1;
        }

        if (string.Equals(current!.SelfExtend.Mode, mode, StringComparison.Ordinal))
        {
            stdout.WriteLine($"self-extend mode is already '{mode}' — nothing changed.");
            return 0;
        }

        if (!TryReplaceSelfExtendMode(original, mode, out var edited))
        {
            stderr.WriteLine("could not locate selfExtend.mode in ashlar.policy.yaml; leave the file's structure intact "
                + "or edit it by hand. Nothing was written.");
            return 1;
        }

        // Validate the RESULT before writing. Fail-closed: a flip that would produce an invalid policy
        // (self-extending / proposing with no gatesRequired) is refused with nothing written.
        if (!PolicyLoader.TryLoad(edited, out var reparsed, out var newReason))
        {
            stderr.WriteLine($"setting mode to '{mode}' would make the policy invalid: {newReason} Nothing was written.");
            return 1;
        }

        // Trust the OUTCOME, not the textual edit. If the effective mode after the edit is not what was
        // asked — the classic cause is a duplicate selfExtend.mode entry, which YAML resolves last-wins
        // while the edit changed a different occurrence — refuse. A `disarm` that silently left a node
        // armed (or an arm that silently did nothing) is the worst failure this command could have.
        if (!string.Equals(reparsed!.SelfExtend.Mode, mode, StringComparison.Ordinal))
        {
            stderr.WriteLine($"the edit did not take effect — the effective mode is still '{reparsed.SelfExtend.Mode}'. "
                + "ashlar.policy.yaml likely has a duplicate selfExtend.mode entry; fix it by hand. Nothing was written. "
                + "(To stop a running node now regardless, use `ashlar background-agent disarm`.)");
            return 1;
        }

        File.WriteAllText(policyPath, edited);

        stdout.WriteLine($"self-extend mode: {current.SelfExtend.Mode} → {mode}");
        stdout.WriteLine(mode switch
        {
            SelfExtendMode.SelfExtending =>
                "  ARMED: this node will now AUTO-APPLY admitted self-extend cycles within budget "
                + $"({current.SelfExtend.Budget.Extensions} per {current.SelfExtend.Budget.Window}), gated by the post-apply "
                + "canary (a change that fails verification is rolled back). Disarm with `ashlar policy set self_extend proposing` "
                + "or stop everything now with `ashlar background-agent disarm`."
                + (current.SelfExtend.Budget.Extensions == 0
                    ? "\n  NOTE: budget is 0 per window — nothing will auto-admit until you raise selfExtend.budget.extensions."
                    : string.Empty),
            SelfExtendMode.Proposing =>
                "  Self-extend cycles are now HELD for review (`ashlar gates`); no auto-apply.",
            SelfExtendMode.Sealed =>
                "  Sealed: no self-extension will be admitted at all.",
            _ => string.Empty,
        });
        stdout.WriteLine("  (The daemon re-reads the policy each cycle — no restart needed.)");
        return 0;
    }

    /// <summary>
    /// Replaces the value of the <c>mode:</c> line WITHIN the <c>selfExtend:</c> block, preserving
    /// every other line, comment, indentation, trailing comment, and the file's newline style. Scoped
    /// to the selfExtend block so a <c>mode:</c> under any other key is never touched. Returns false if
    /// the block or its mode line cannot be found (caller then writes nothing).
    /// </summary>
    public static bool TryReplaceSelfExtendMode(string text, string newMode, out string result)
    {
        result = text;
        // Rejoin with the file's dominant newline, but SPLIT on any ending so a mixed CRLF/LF file
        // (which still loads) is scanned correctly rather than spuriously refused.
        var newline = text.Contains("\r\n") ? "\r\n" : "\n";
        var lines = Regex.Split(text, @"\r\n|\r|\n");

        int selfExtendIdx = -1, selfExtendIndent = -1;
        for (var i = 0; i < lines.Length; i++)
        {
            var m = Regex.Match(lines[i], @"^(\s*)selfExtend:\s*(#.*)?$");
            if (m.Success)
            {
                selfExtendIdx = i;
                selfExtendIndent = m.Groups[1].Value.Length;
                break;
            }
        }
        if (selfExtendIdx < 0)
        {
            return false;   // no selfExtend block
        }

        for (var i = selfExtendIdx + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Trim().Length == 0 || line.TrimStart().StartsWith('#'))
            {
                continue;   // blank / comment lines belong to whatever block they sit in
            }
            var indent = line.Length - line.TrimStart().Length;
            if (indent <= selfExtendIndent)
            {
                return false;   // reached a sibling/parent key before finding mode — block has no mode line
            }
            var mm = Regex.Match(line, @"^(\s*mode:\s*)(\S+)(.*)$");
            if (mm.Success)
            {
                lines[i] = mm.Groups[1].Value + newMode + mm.Groups[3].Value;
                result = string.Join(newline, lines);
                return true;
            }
        }
        return false;   // block ended (EOF) without a mode line
    }
}
