using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Abstractions.Agents;

namespace Ashlar.Orchestration.Metrics;

/// <summary>
/// Metrics for agent execution.
/// 
/// Contains:
/// - Agent ID and domain
/// - Execution statistics (count, duration min/max/total)
/// - Last execution state
/// - Resource usage (memory, context tokens)
/// 
/// Used by OrchestrationMetrics to track agent performance.
/// </summary>
public sealed class AgentMetrics
{
    /// <summary>Unique agent identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Agent domain or specialty.</summary>
    public required string Domain { get; init; }

    /// <summary>Total number of recorded executions.</summary>
    public int ExecutionCount { get; set; }

    /// <summary>Cumulative execution duration.</summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>Shortest observed execution duration.</summary>
    public TimeSpan MinDuration { get; set; }

    /// <summary>Longest observed execution duration.</summary>
    public TimeSpan MaxDuration { get; set; }

    /// <summary>Most recent agent state.</summary>
    public AgentState LastState { get; set; }

    /// <summary>Cumulative memory usage in megabytes.</summary>
    public long TotalResourceUsageMB { get; set; }

    /// <summary>Cumulative context tokens consumed.</summary>
    public long TotalContextTokensUsed { get; set; }
}
