namespace Nexo.Core.Application.Analysis.Models;

/// <summary>
/// Result of an analysis operation.
/// </summary>
public record AnalysisResult
{
    public required bool HasViolations { get; init; }
    public required IReadOnlyList<Violation> Violations { get; init; }
    public required int TotalViolations { get; init; }
}

/// <summary>
/// Represents a single violation found during analysis.
/// </summary>
public record Violation
{
    public required string Rule { get; init; }
    public required string Message { get; init; }
    public required string FilePath { get; init; }
    public int? LineNumber { get; init; }
    public string? Severity { get; init; }
}

