namespace Ashlar.Abstractions.Agents;

/// <summary>
/// Represents the lifecycle state of an orchestration agent.
/// </summary>
public enum AgentState
{
    /// <summary>Agent instance has been constructed but not yet initialized.</summary>
    Created,

    /// <summary>Agent is loading dependencies, configuration, or runtime resources.</summary>
    Initializing,

    /// <summary>Agent is blocked until upstream dependency outputs are available.</summary>
    WaitingForDependencies,

    /// <summary>Agent is initialized and available to accept invocation requests.</summary>
    Ready,

    /// <summary>Agent is actively processing an invocation.</summary>
    Executing,

    /// <summary>Agent finished the current invocation successfully.</summary>
    Completed,

    /// <summary>Agent encountered an unrecoverable error during initialization or execution.</summary>
    Failed,

    /// <summary>Agent is draining work and releasing resources in a controlled shutdown.</summary>
    ShuttingDown,

    /// <summary>Agent has been forcefully terminated and is no longer operational.</summary>
    Terminated
}
