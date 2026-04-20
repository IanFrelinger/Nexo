using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Container wrapper for agent execution, providing isolation and resource management.
/// 
/// Responsibilities:
/// - Wraps BaseAgent instances for orchestration
/// - Tracks execution timing (start/end times)
/// - Manages resource usage statistics
/// - Provides lifecycle methods (Initialize, Execute, Shutdown, Terminate)
/// - Handles dependency waiting before execution
/// 
/// Used by LifecycleManager and Orchestrator to manage agent execution.
/// </summary>
public sealed class AgentContainer
{
    private readonly BaseAgent _agent;
    private readonly ILogger<AgentContainer> _logger;
    private readonly ResourceRequirements? _resourceRequirements;
    private DateTimeOffset? _startTime;
    private DateTimeOffset? _endTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentContainer"/> class.
    /// </summary>
    /// <param name="agent">The agent to wrap.</param>
    /// <param name="logger">The logger instance.</param>
    public AgentContainer(BaseAgent agent, ILogger<AgentContainer> logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourceRequirements = agent.Spec.ResourceRequirements;
    }

    /// <summary>
    /// Gets the agent's ID.
    /// </summary>
    public string AgentId => _agent.Spec.AgentId;

    /// <summary>
    /// Gets the agent's current state.
    /// </summary>
    public AgentState State => _agent.State;

    /// <summary>
    /// Gets the agent's current health status.
    /// </summary>
    public AgentHealth Health => _agent.Health;

    /// <summary>
    /// Gets the wrapped agent instance.
    /// </summary>
    public BaseAgent Agent => _agent;

    /// <summary>
    /// Gets resource usage statistics for the agent.
    /// 
    /// Calculates duration from start time to end time (or current time if still running).
    /// Includes estimated compute, context tokens, and memory requirements from the agent spec.
    /// </summary>
    /// <returns>A <see cref="ResourceUsage"/> record with usage statistics.</returns>
    public ResourceUsage GetResourceUsage()
    {
        var duration = _endTime.HasValue && _startTime.HasValue
            ? _endTime.Value - _startTime.Value
            : (_startTime.HasValue ? DateTimeOffset.UtcNow - _startTime.Value : TimeSpan.Zero);

        return new ResourceUsage
        {
            AgentId = AgentId,
            Duration = duration,
            EstimatedComputeSeconds = _resourceRequirements?.EstimatedComputeSeconds,
            RequiredContextTokens = _resourceRequirements?.RequiredContextTokens,
            RequiredMemoryMB = _resourceRequirements?.RequiredMemoryMB
        };
    }

    /// <summary>
    /// Initializes the agent container.
    /// 
    /// Records the start time and calls the agent's InitializeAsync method.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _startTime = DateTimeOffset.UtcNow;
        await _agent.InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// Waits for dependencies and then executes the agent.
    /// 
    /// If dependency outputs are provided and the agent has dependencies,
    /// waits for dependencies to be resolved before executing.
    /// Records the end time after execution completes.
    /// </summary>
    /// <param name="dependencyOutputs">Optional dictionary of dependency agent IDs to their outputs.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The agent's execution output.</returns>
    public async Task<object> ExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs = null,
        CancellationToken cancellationToken = default)
    {
        if (dependencyOutputs != null && _agent.Spec.Dependencies.Count > 0)
        {
            await _agent.WaitForDependenciesAsync(dependencyOutputs, cancellationToken);
        }

        var result = await _agent.ExecuteAsync(dependencyOutputs, cancellationToken);
        _endTime = DateTimeOffset.UtcNow;
        return result;
    }

    /// <summary>
    /// Shuts down the agent container.
    /// 
    /// Calls the agent's ShutdownAsync method and records the end time.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous shutdown operation.</returns>
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        await _agent.ShutdownAsync(cancellationToken);
        _endTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Forces termination of the agent (use when shutdown fails or timeout occurs).
    /// 
    /// Records the end time immediately. The agent's state will be set to Terminated
    /// by the shutdown process. This method is called when graceful shutdown fails.
    /// </summary>
    public void Terminate()
    {
        _logger.LogWarning("Force terminating agent {AgentId}", AgentId);
        _endTime = DateTimeOffset.UtcNow;
        // Agent state will be set to Terminated by shutdown
    }
}

/// <summary>
/// Resource usage statistics for an agent.
/// </summary>
public sealed record ResourceUsage
{
    /// <summary>
    /// Gets the agent ID.
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    /// Gets the execution duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the estimated compute seconds (from agent spec).
    /// </summary>
    public int? EstimatedComputeSeconds { get; init; }

    /// <summary>
    /// Gets the required context tokens (from agent spec).
    /// </summary>
    public int? RequiredContextTokens { get; init; }

    /// <summary>
    /// Gets the required memory in MB (from agent spec).
    /// </summary>
    public int? RequiredMemoryMB { get; init; }
}

