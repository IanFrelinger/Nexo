using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Agents;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Health status information for an agent.
/// </summary>
public sealed class AgentHealthStatus
{
    /// <summary>Unique agent identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Runtime container hosting the agent.</summary>
    public required AgentContainer Container { get; init; }

    /// <summary>Current health assessment.</summary>
    public AgentHealth Health { get; set; }

    /// <summary>Current execution state.</summary>
    public AgentState State { get; set; }

    /// <summary>UTC timestamp of the most recent health check.</summary>
    public DateTimeOffset LastHealthCheck { get; set; }

    /// <summary>UTC timestamp of the most recent state transition.</summary>
    public DateTimeOffset? LastStateChange { get; set; }
}
