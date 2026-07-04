namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Aggregated result from multiple test instances.
/// </summary>
public record AggregatedResult
{
    /// <summary>Individual results from each test instance.</summary>
    public required IReadOnlyList<InstanceResult> Results { get; init; }

    /// <summary>Best passing candidate, if any instance succeeded.</summary>
    public InstanceResult? BestCandidate { get; init; }

    /// <summary>Whether every instance passed.</summary>
    public bool AllPassed { get; init; }
}
