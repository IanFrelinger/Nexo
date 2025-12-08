using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents.Models;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Base class for specialized agents spawned from AgentSpawnSpec.
/// </summary>
public abstract class BaseAgent : IAgent
{
    private readonly AgentSpawnSpec _spec;
    private readonly ILogger<BaseAgent> _logger;
    private AgentState _state = AgentState.Created;
    private AgentHealth _health = AgentHealth.Healthy;
    private DateTimeOffset? _startedAt;
    private DateTimeOffset? _completedAt;
    private object? _output;

    /// <summary>
    /// Protected logger for derived classes.
    /// </summary>
    protected ILogger<BaseAgent> Logger => _logger;

    protected BaseAgent(AgentSpawnSpec spec, ILogger<BaseAgent> logger)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => _spec.AgentId;

    public AgentSpawnSpec Spec => _spec;

    public AgentState State
    {
        get => _state;
        protected set
        {
            if (_state != value)
            {
                var oldState = _state;
                _state = value;
                _logger.LogDebug("Agent {AgentId} state changed: {OldState} -> {NewState}", _spec.AgentId, oldState, value);
                OnStateChanged(oldState, value);
            }
        }
    }

    public AgentHealth Health
    {
        get => _health;
        protected set
        {
            if (_health != value)
            {
                var oldHealth = _health;
                _health = value;
                _logger.LogDebug("Agent {AgentId} health changed: {OldHealth} -> {NewHealth}", _spec.AgentId, oldHealth, value);
                OnHealthChanged(oldHealth, value);
            }
        }
    }

    public DateTimeOffset? StartedAt => _startedAt;
    public DateTimeOffset? CompletedAt => _completedAt;
    public object? Output => _output;

    /// <summary>
    /// Initializes the agent (loads resources, validates constraints).
    /// </summary>
    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (State != AgentState.Created)
        {
            throw new InvalidOperationException($"Agent {_spec.AgentId} cannot be initialized from state {State}");
        }

        State = AgentState.Initializing;
        _startedAt = DateTimeOffset.UtcNow;

        try
        {
            await OnInitializeAsync(cancellationToken);
            State = AgentState.Ready;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} initialization failed", _spec.AgentId);
            State = AgentState.Failed;
            Health = AgentHealth.Unhealthy;
            throw;
        }
    }

    /// <summary>
    /// Waits for dependencies to be resolved.
    /// </summary>
    public virtual async Task WaitForDependenciesAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken = default)
    {
        if (State != AgentState.WaitingForDependencies && State != AgentState.Ready)
        {
            return;
        }

        State = AgentState.WaitingForDependencies;

        // Check if all dependencies are available
        var missingDependencies = _spec.Dependencies
            .Where(dep => !dependencyOutputs.ContainsKey(dep))
            .ToList();

        if (missingDependencies.Count > 0)
        {
            _logger.LogDebug("Agent {AgentId} still waiting for dependencies: {Dependencies}",
                _spec.AgentId, string.Join(", ", missingDependencies));
            return;
        }

        await OnDependenciesResolvedAsync(dependencyOutputs, cancellationToken);
        State = AgentState.Ready;
    }

    /// <summary>
    /// Executes the agent's task.
    /// </summary>
    public virtual async Task<object> ExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs = null,
        CancellationToken cancellationToken = default)
    {
        if (State != AgentState.Ready)
        {
            throw new InvalidOperationException($"Agent {_spec.AgentId} cannot execute from state {State}");
        }

        State = AgentState.Executing;

        try
        {
            var result = await OnExecuteAsync(dependencyOutputs, cancellationToken);
            _output = result;
            State = AgentState.Completed;
            _completedAt = DateTimeOffset.UtcNow;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} execution failed", _spec.AgentId);
            State = AgentState.Failed;
            Health = AgentHealth.Unhealthy;
            throw;
        }
    }

    /// <summary>
    /// Shuts down the agent gracefully.
    /// </summary>
    public virtual async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        if (State == AgentState.Terminated || State == AgentState.ShuttingDown)
        {
            return;
        }

        State = AgentState.ShuttingDown;

        try
        {
            await OnShutdownAsync(cancellationToken);
            State = AgentState.Terminated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} shutdown failed", _spec.AgentId);
            State = AgentState.Terminated; // Force termination even if shutdown fails
            throw;
        }
    }

    /// <summary>
    /// Implements IAgent.ThinkAsync - delegates to ExecuteAsync.
    /// </summary>
    public async Task<AgentActions> ThinkAsync(
        AgentObservation obs,
        IToolbox tools,
        IAgentMemory mem,
        CancellationToken ct)
    {
        // For orchestrated agents, we use ExecuteAsync instead
        // This method is kept for IAgent compatibility
        if (State == AgentState.Ready)
        {
            await ExecuteAsync(null, ct);
        }

        return AgentActions.None;
    }

    // Abstract methods for subclasses to implement
    protected abstract Task OnInitializeAsync(CancellationToken cancellationToken);
    protected abstract Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken);
    protected abstract Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken);
    protected abstract Task OnShutdownAsync(CancellationToken cancellationToken);

    // Virtual methods for subclasses to override
    protected virtual void OnStateChanged(AgentState oldState, AgentState newState) { }
    protected virtual void OnHealthChanged(AgentHealth oldHealth, AgentHealth newHealth) { }
}

