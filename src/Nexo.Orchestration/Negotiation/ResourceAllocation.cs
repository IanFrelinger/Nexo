using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Negotiation;

/// <summary>
/// Resource allocation for multiple agents.
/// </summary>
public sealed record ResourceAllocation
{
    public IReadOnlyDictionary<string, AllocatedResources> Allocations { get; init; } =
        new Dictionary<string, AllocatedResources>();
}
