namespace Nexo.Abstractions.Agents;

/// <summary>
/// Runtime handle for a spawned orchestration agent (composition root over in-process or remote execution).
/// </summary>
public interface IAgentHandle
{
    /// <summary>Stable identifier for this agent instance within the orchestration scope.</summary>
    string AgentId { get; }

    /// <summary>Current lifecycle state of the agent.</summary>
    AgentState State { get; }

    /// <summary>Operational health signal derived from runtime checks and recent execution outcomes.</summary>
    AgentHealth Health { get; }

    /// <summary>
    /// Prepares the agent for execution by loading dependencies and transitioning to <see cref="AgentState.Ready"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the agent's primary work unit, optionally supplying outputs from upstream dependencies.
    /// </summary>
    /// <param name="dependencyOutputs">Outputs from prerequisite agents keyed by dependency name.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    /// <returns>The agent's execution result payload.</returns>
    Task<object> ExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gracefully stops the agent, allowing in-flight work to complete before releasing resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Immediately terminates the agent without waiting for graceful shutdown.
    /// Transitions the handle to <see cref="AgentState.Terminated"/>.
    /// </summary>
    void Terminate();
}
