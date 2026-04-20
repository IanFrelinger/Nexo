namespace Nexo.Abstractions.Agents;

/// <summary>
/// Represents the lifecycle state of an orchestration agent.
/// </summary>
public enum AgentState
{
    Created,
    Initializing,
    WaitingForDependencies,
    Ready,
    Executing,
    Completed,
    Failed,
    ShuttingDown,
    Terminated
}

/// <summary>
/// Represents orchestration agent health status.
/// </summary>
public enum AgentHealth
{
    Healthy,
    Degraded,
    Unhealthy
}
