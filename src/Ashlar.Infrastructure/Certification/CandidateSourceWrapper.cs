namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// The single wrap applied to candidate brick source before any certification-path compile.
///
/// Extracted from <see cref="BrickMutationEngine"/> so the analyzer gate and the mutation
/// engine compile byte-identical candidate text (spec A1.2: analyzer and compiler must see the
/// same bytes). The wrap prepends the global usings generated bricks rely on and appends the
/// deterministic <c>CertAuditContext</c> the witness runner drives mutants with.
/// </summary>
/// <remarks>
/// <para>Both injections are UNCONDITIONAL, and that is the whole point. The wrap used to look
/// for <c>namespace</c> and then for the brace after it, and inject the audit context inside
/// that brace — so two ordinary candidate shapes silently got no audit context at all: a brick
/// with NO namespace (the first thing a newcomer writes), and a file-scoped namespace with no
/// braced type after it. Those candidates still compiled, so nothing complained. What broke was
/// the mutation leg: the in-process mutant executor of the time constructed the execution
/// context by finding <c>CertAuditContext</c> in the mutant assembly, so every mutant threw before
/// it ran a single witness case, every throw was scored as a KILL, <c>escape_rate</c> came out 0.0,
/// and the gate signed a record asserting the witness had teeth when the leg had proved nothing.
/// (The replay runner now supplies its own context and needs nothing from the candidate, but the
/// wrap stays unconditional: the analyzer fence and the compiler must keep seeing one text.)</para>
///
/// <para>Appending at the end of the file — rather than threading the text for an insertion
/// point — is what makes the injection unconditional. The audit context references nothing in
/// the candidate and the candidate references nothing in it, so where it lands is irrelevant to
/// both: a block-scoped namespace leaves it at global scope, a file-scoped one absorbs it, and
/// either way reflection finds it by name. It also means candidate line numbers are shifted by
/// the preamble alone, so <see cref="MapToCandidateLine"/> is a subtraction rather than a
/// second heuristic that could disagree with the first.</para>
/// </remarks>
internal static class CandidateSourceWrapper
{
    private const string SystemUsings = """
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainBrick = Ashlar.Core.Domain.Bricks.Brick;

""";

    private const string AuditContext = """

internal sealed class CertAuditContext : Ashlar.Core.Domain.Execution.IExecutionContext
{
    public string AgentId => "cert-gate";
    public string BehaviorId => "cert-gate";
    public bool IsAirGapped => true;
    public bool AuditMode => true;
    public string Provider => "deterministic";
    public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
}

""";

    /// <summary>The type name the mutation harness resolves out of every wrapped compile.</summary>
    public const string AuditContextTypeName = "CertAuditContext";

    /// <summary>Line count the usings preamble shifts every candidate line by.</summary>
    public static int PreambleLineCount { get; } = CountLines(SystemUsings);

    /// <summary>Line count of the appended audit context. It follows the candidate, so it
    /// shifts no candidate line.</summary>
    public static int AuditContextLineCount { get; } = CountLines(AuditContext);

    /// <summary>Wraps candidate source exactly as every certification-path compile does.</summary>
    public static string Wrap(string sourceCode)
        => SystemUsings + sourceCode + AuditContext;

    /// <summary>
    /// Maps a 1-based line number in the wrapped text back to the candidate's own coordinates,
    /// so analyzer feedback points at lines the proposer actually wrote. Lines inside the
    /// injected preamble or the appended audit block map to line 0 ("not in candidate source").
    /// </summary>
    public static int MapToCandidateLine(string sourceCode, int wrappedLine)
    {
        var afterPreamble = wrappedLine - PreambleLineCount;
        if (afterPreamble <= 0)
            return 0;

        // Past the end of the candidate is the appended audit context, which the proposer did
        // not write and must never be pointed at.
        var candidateLines = CountLines(sourceCode) + (sourceCode.EndsWith('\n') ? 0 : 1);
        return afterPreamble > candidateLines ? 0 : afterPreamble;
    }

    private static int CountLines(string text)
    {
        var count = 0;
        foreach (var c in text)
        {
            if (c == '\n')
                count++;
        }

        return count;
    }
}
