namespace Nexo.Orchestration.Agents.Models;

/// <summary>
/// Represents the lifecycle state of an agent.
/// </summary>
public enum AgentState
{
    /// <summary>
    /// Agent has been created but not started.
    /// </summary>
    Created,

    /// <summary>
    /// Agent is initializing (loading resources, connecting to dependencies).
    /// </summary>
    Initializing,

    /// <summary>
    /// Agent is waiting for dependencies to be resolved.
    /// </summary>
    WaitingForDependencies,

    /// <summary>
    /// Agent is ready and can execute tasks.
    /// </summary>
    Ready,

    /// <summary>
    /// Agent is currently executing a task.
    /// </summary>
    Executing,

    /// <summary>
    /// Agent has completed its task and produced output.
    /// </summary>
    Completed,

    /// <summary>
    /// Agent execution failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Agent is being shut down gracefully.
    /// </summary>
    ShuttingDown,

    /// <summary>
    /// Agent has been terminated.
    /// </summary>
    Terminated
}

/// <summary>
/// Represents agent health status.
/// </summary>
public enum AgentHealth
{
    /// <summary>
    /// Agent is healthy and functioning normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// Agent is degraded but still functional.
    /// </summary>
    Degraded,

    /// <summary>
    /// Agent is unhealthy and may not function correctly.
    /// </summary>
    Unhealthy
}

