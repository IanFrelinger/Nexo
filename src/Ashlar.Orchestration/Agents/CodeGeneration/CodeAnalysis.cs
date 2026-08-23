using Microsoft.Extensions.Logging;

namespace Ashlar.Orchestration.Agents.CodeGeneration;

/// <summary>
/// Result of code analysis.
/// </summary>
public sealed record CodeAnalysis
{
    /// <summary>Programming language of the analyzed code.</summary>
    public required string Language { get; init; }

    /// <summary>Total line count in the source.</summary>
    public required int LineCount { get; init; }

    /// <summary>Cyclomatic or heuristic complexity score.</summary>
    public required int Complexity { get; init; }

    /// <summary>Issues discovered during analysis.</summary>
    public IReadOnlyList<CodeIssue> Issues { get; init; } = new List<CodeIssue>();
}
