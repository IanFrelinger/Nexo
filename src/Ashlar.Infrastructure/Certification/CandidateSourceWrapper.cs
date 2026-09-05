namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// The single wrap applied to candidate brick source before any certification-path compile.
///
/// Extracted from <see cref="BrickMutationEngine"/> so the analyzer gate and the mutation
/// engine compile byte-identical candidate text (spec A1.2: analyzer and compiler must see the
/// same bytes). The wrap prepends the global usings generated bricks rely on and injects the
/// deterministic <c>CertAuditContext</c> the witness runner drives mutants with.
/// </summary>
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

    /// <summary>Line count the usings preamble shifts every candidate line by.</summary>
    public static int PreambleLineCount { get; } = CountLines(SystemUsings);

    /// <summary>Line count the audit-context insertion additionally shifts post-namespace-brace lines by.</summary>
    public static int AuditContextLineCount { get; } = CountLines(AuditContext);

    /// <summary>Wraps candidate source exactly as every certification-path compile does.</summary>
    public static string Wrap(string sourceCode)
    {
        var namespaceIndex = sourceCode.IndexOf("namespace ", StringComparison.Ordinal);
        if (namespaceIndex < 0)
            return SystemUsings + sourceCode;

        var braceIndex = sourceCode.IndexOf('{', namespaceIndex);
        if (braceIndex < 0)
            return SystemUsings + sourceCode;

        return SystemUsings + sourceCode.Insert(braceIndex + 1, AuditContext);
    }

    /// <summary>
    /// Maps a 1-based line number in the wrapped text back to the candidate's own coordinates,
    /// so analyzer feedback points at lines the proposer actually wrote. Lines inside the
    /// injected preamble/audit block map to line 0 ("not in candidate source").
    /// </summary>
    public static int MapToCandidateLine(string sourceCode, int wrappedLine)
    {
        var afterPreamble = wrappedLine - PreambleLineCount;
        if (afterPreamble <= 0)
            return 0;

        var namespaceIndex = sourceCode.IndexOf("namespace ", StringComparison.Ordinal);
        var braceIndex = namespaceIndex < 0 ? -1 : sourceCode.IndexOf('{', namespaceIndex);
        if (braceIndex < 0)
            return afterPreamble;

        // The audit context is inserted immediately after the namespace brace; every wrapped
        // line beyond that point carries both offsets.
        var braceLine = CountLines(sourceCode.Substring(0, braceIndex + 1));
        if (afterPreamble <= braceLine)
            return afterPreamble;

        var pastAudit = afterPreamble - AuditContextLineCount;
        return pastAudit <= braceLine ? 0 : pastAudit;
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
