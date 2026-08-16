using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Where a mutation was applied and what it changed, in the candidate's own coordinates.
/// </summary>
/// <remarks>
/// A surviving mutant id names an operator and a line — enough to locate, not enough to
/// adjudicate. Deciding whether a survivor is a weak witness or an EQUIVALENT MUTANT (a
/// rewrite that cannot change behaviour on any input, so no witness case could ever kill it)
/// needs the actual edit. Campaign 2 (ledger S5) hit this twice on <c>semver-parse</c>:
/// <c>if (version.Length &gt; 0)</c> mutated to <c>&gt; 2</c>, and on a structurally different
/// proposal <c>if (prereleaseIndex &gt;= 0)</c> mutated to <c>&gt;= 2</c> — both redundant
/// guards, both unkillable, and neither distinguishable from a real escape by id alone.
/// </remarks>
/// <param name="Line">1-based line in the candidate source.</param>
/// <param name="Column">1-based column, which disambiguates two mutations on one line.</param>
/// <param name="OriginalText">The code as written.</param>
/// <param name="MutatedText">What it was rewritten to.</param>
/// <param name="LineText">The whole source line, trimmed, for context.</param>
internal sealed record MutationSite(
    int Line,
    int Column,
    string OriginalText,
    string MutatedText,
    string LineText);

/// <summary>A surviving mutant: its id and the edit that produced it.</summary>
internal sealed record MutationSurvivor(string Id, MutationSite Site)
{
    /// <summary>
    /// One operator-facing line: id, location, the edit, and the line it landed on.
    /// </summary>
    public string Describe() =>
        $"{Id} (line {Site.Line}, col {Site.Column}): {Site.OriginalText} -> {Site.MutatedText}"
        + (string.IsNullOrWhiteSpace(Site.LineText) ? string.Empty : $"  in: {Site.LineText}");
}

internal sealed record AstMutation(string Id, SyntaxNode MutatedRoot, MutationSite Site)
{
    /// <summary>To source.</summary>
    public string ToSource() => MutatedRoot.NormalizeWhitespace().ToFullString();
}
