using Ashlar.Core.Domain.Values;

namespace Ashlar.Core.Application.Analysis.Models;

/// <summary>
/// Result of static analysis on brick/code (schema, safety, performance).
/// </summary>
public record BrickStaticAnalysisResult
{
    /// <summary>Whether static analysis passed with no blocking violations.</summary>
    public required bool Passed { get; init; }

    /// <summary>Violations found during static analysis.</summary>
    public required IReadOnlyList<Violation> Violations { get; init; }

    /// <summary>Total number of violations in <see cref="Violations"/>.</summary>
    public int TotalViolations => Violations.Count;
}
