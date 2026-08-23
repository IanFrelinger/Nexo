using Ashlar.Core.Domain.Values;

namespace Ashlar.Core.Application.Analysis.Models;

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
    /// <summary>Whether any violations were found.</summary>
    public required bool HasViolations { get; init; }

    /// <summary>Violations discovered during analysis.</summary>
    public required IReadOnlyList<Violation> Violations { get; init; }

    /// <summary>Total number of violations in <see cref="Violations"/>.</summary>
    public required int TotalViolations { get; init; }
}
