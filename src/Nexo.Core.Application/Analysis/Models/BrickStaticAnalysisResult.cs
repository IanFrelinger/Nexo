using Nexo.Core.Domain.Values;

namespace Nexo.Core.Application.Analysis.Models;

/// <summary>
/// Result of static analysis on brick/code (schema, safety, performance).
/// </summary>
public record BrickStaticAnalysisResult
{
    public required bool Passed { get; init; }
    public required IReadOnlyList<Violation> Violations { get; init; }
    public int TotalViolations => Violations.Count;
}
