using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Architect;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Negotiation;

/// <summary>
/// Allocated resources for a single agent.
/// </summary>
public sealed record AllocatedResources
{
    /// <summary>Compute budget in seconds.</summary>
    public int ComputeSeconds { get; init; }

    /// <summary>Memory budget in megabytes.</summary>
    public int MemoryMb { get; init; }

    /// <summary>Context token budget.</summary>
    public int ContextTokens { get; init; }
}
