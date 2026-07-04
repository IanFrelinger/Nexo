using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Resource allocation for an agent.
/// 
/// Contains:
/// - Agent ID
/// - Allocated compute seconds, context tokens, and memory
/// - Priority level
/// 
/// Created by ResourceAllocator when resources are allocated to an agent.
/// </summary>
public sealed record ResourceAllocation
{
    /// <summary>Agent receiving the allocation.</summary>
    public required string AgentId { get; init; }

    /// <summary>Allocated compute budget in seconds.</summary>
    public int AllocatedComputeSeconds { get; init; }

    /// <summary>Allocated context token budget.</summary>
    public int AllocatedContextTokens { get; init; }

    /// <summary>Allocated memory budget in megabytes.</summary>
    public int AllocatedMemoryMB { get; init; }

    /// <summary>Scheduling priority for the agent.</summary>
    public int Priority { get; init; }
}
