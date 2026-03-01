namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Aggregated result from multiple test instances.
/// </summary>
public record AggregatedResult
{
    public required IReadOnlyList<InstanceResult> Results { get; init; }
    public InstanceResult? BestCandidate { get; init; }
    public bool AllPassed { get; init; }
}
