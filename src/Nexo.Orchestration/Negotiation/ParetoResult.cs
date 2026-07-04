using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Negotiation;

/// <summary>
/// Result of Pareto optimization.
/// </summary>
public sealed record ParetoResult
{
    /// <summary>Whether competing resource demands conflict.</summary>
    public bool HasConflict { get; init; }

    /// <summary>Non-dominated allocations on the Pareto frontier.</summary>
    public IReadOnlyList<ResourceAllocation> ParetoFrontier { get; init; } = Array.Empty<ResourceAllocation>();

    /// <summary>Recommended allocation from the frontier.</summary>
    public ResourceAllocation? RecommendedAllocation { get; init; }

    /// <summary>Tradeoff descriptions between frontier options.</summary>
    public IReadOnlyList<string> Tradeoffs { get; init; } = Array.Empty<string>();

    public static ParetoResult NoConflict() => new() { HasConflict = false };
}
