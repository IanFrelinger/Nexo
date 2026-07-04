using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using Nexo.Abstractions.Agents;

namespace Nexo.Orchestration.Metrics;

/// <summary>
/// Report for agent execution.
/// 
/// Contains:
/// - Agent ID and domain
/// - Execution statistics (count, duration min/max/average)
/// - Resource usage averages (memory, context tokens)
/// 
/// Used by PerformanceReport to report agent metrics.
/// </summary>
public sealed record AgentReport
{
    /// <summary>Unique agent identifier.</summary>
    public required string AgentId { get; init; }

    /// <summary>Agent domain or specialty.</summary>
    public required string Domain { get; init; }

    /// <summary>Total number of recorded executions.</summary>
    public required int ExecutionCount { get; init; }

    /// <summary>Mean execution duration.</summary>
    public required TimeSpan AverageDuration { get; init; }

    /// <summary>Shortest observed execution duration.</summary>
    public required TimeSpan MinDuration { get; init; }

    /// <summary>Longest observed execution duration.</summary>
    public required TimeSpan MaxDuration { get; init; }

    /// <summary>Most recent agent state at report time.</summary>
    public required AgentState LastState { get; init; }

    /// <summary>Average memory usage in megabytes.</summary>
    public required long AverageResourceUsageMB { get; init; }

    /// <summary>Average context tokens consumed per execution.</summary>
    public required long AverageContextTokensUsed { get; init; }
}
