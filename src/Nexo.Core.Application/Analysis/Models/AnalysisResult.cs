using Nexo.Core.Domain.Values;

namespace Nexo.Core.Application.Analysis.Models;

/// <summary>
/// Result of an analysis operation.
/// 
/// Contains:
/// - Whether violations were found
/// - List of violations
/// - Total violation count
/// 
/// Produced by IAnalysisService after analyzing code/assemblies.
/// Used by CLI commands to display analysis results.
/// </summary>
public record AnalysisResult
{
    public required bool HasViolations { get; init; }
    public required IReadOnlyList<Violation> Violations { get; init; }
    public required int TotalViolations { get; init; }
}

/// <summary>
/// Represents a single violation found during analysis.
/// 
/// Contains:
/// - Rule that was violated
/// - Human-readable message
/// - File path and line number (if applicable)
/// - Severity level (RiskLevel)
/// 
/// Used to report specific issues found during code analysis.
/// </summary>
public record Violation
{
    public required string Rule { get; init; }
    public required string Message { get; init; }
    public required string FilePath { get; init; }
    public int? LineNumber { get; init; }
    public RiskLevel Severity { get; init; } = RiskLevel.Medium;
}

