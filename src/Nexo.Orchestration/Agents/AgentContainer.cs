using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents.Models;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Container wrapper for agent execution, providing isolation and resource management.
/// </summary>
public sealed class AgentContainer
{
    private readonly BaseAgent _agent;
    private readonly ILogger<AgentContainer> _logger;
    private readonly ResourceRequirements? _resourceRequirements;
    private DateTimeOffset? _startTime;
    private DateTimeOffset? _endTime;

    public AgentContainer(BaseAgent agent, ILogger<AgentContainer> logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourceRequirements = agent.Spec.ResourceRequirements;
    }

    public string AgentId => _agent.Spec.AgentId;
    public AgentState State => _agent.State;
    public AgentHealth Health => _agent.Health;
    public BaseAgent Agent => _agent;

    /// <summary>
    /// Gets resource usage statistics.
    /// </summary>
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
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _startTime = DateTimeOffset.UtcNow;
        await _agent.InitializeAsync(cancellationToken);
    }

    /// <summary>
    /// Waits for dependencies and then executes the agent.
    /// </summary>
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
    /// </summary>
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        await _agent.ShutdownAsync(cancellationToken);
        _endTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Forces termination of the agent (use when shutdown fails or timeout occurs).
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
    public required string AgentId { get; init; }
    public TimeSpan Duration { get; init; }
    public int? EstimatedComputeSeconds { get; init; }
    public int? RequiredContextTokens { get; init; }
    public int? RequiredMemoryMB { get; init; }
}

