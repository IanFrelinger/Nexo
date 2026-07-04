using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Progress information for an agent.
/// 
/// Contains:
/// - Agent ID
/// - First seen and last update timestamps
/// - Current state and health
/// 
/// Tracked by ProgressTracker to monitor agent execution progress.
/// </summary>
public sealed class AgentProgress
{
    /// <summary>Unique agent identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>UTC timestamp when the agent was first observed.</summary>
    public DateTimeOffset FirstSeen { get; set; }

    /// <summary>UTC timestamp of the most recent progress update.</summary>
    public DateTimeOffset LastUpdate { get; set; }

    /// <summary>Current execution state of the agent.</summary>
    public AgentState CurrentState { get; set; }

    /// <summary>Current health assessment of the agent.</summary>
    public AgentHealth Health { get; set; }
}
