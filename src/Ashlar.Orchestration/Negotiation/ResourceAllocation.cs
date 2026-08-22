using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Architect;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Negotiation;

/// <summary>
/// Resource allocation for multiple agents.
/// </summary>
public sealed record ResourceAllocation
{
    public IReadOnlyDictionary<string, AllocatedResources> Allocations { get; init; } =
        new Dictionary<string, AllocatedResources>();
}
