using Nexo.BackgroundAgents.Configuration;

namespace Nexo.BackgroundAgents.Metrics;

/// <summary>
/// Snapshot of agent execution metrics (for CLI and monitoring).
/// </summary>
/// <param name="AgentId">Agent identifier.</param>
/// <param name="ExecutionCount">Total execution count.</param>
/// <param name="SuccessCount">Successful execution count.</param>
/// <param name="FailureCount">Failed execution count.</param>
/// <param name="SuccessRate">Success rate (0.0-1.0), or null if no executions.</param>
/// <param name="LastStartedAt">When the agent was last started.</param>
/// <param name="LastCompletedAt">When the agent last completed.</param>
/// <param name="LastError">Last error message, if any.</param>
/// <param name="Mode">Current aggressiveness mode (passive, semi-active, active, ambient).</param>
public sealed record AgentMetricsSnapshot(
    string AgentId,
    int ExecutionCount,
    int SuccessCount,
    int FailureCount,
    double? SuccessRate,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastCompletedAt,
    string? LastError,
    BackgroundAgentAggressivenessMode Mode);
