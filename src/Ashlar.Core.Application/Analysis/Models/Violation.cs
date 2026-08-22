using Ashlar.Core.Domain.Values;

namespace Ashlar.Core.Application.Analysis.Models;

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
    /// <summary>Rule identifier that was violated.</summary>
    public required string Rule { get; init; }

    /// <summary>Human-readable description of the violation.</summary>
    public required string Message { get; init; }

    /// <summary>Source file path where the violation was found.</summary>
    public required string FilePath { get; init; }

    /// <summary>Line number in the source file, when available.</summary>
    public int? LineNumber { get; init; }

    /// <summary>Severity level assigned to this violation.</summary>
    public RiskLevel Severity { get; init; } = RiskLevel.Medium;
}
