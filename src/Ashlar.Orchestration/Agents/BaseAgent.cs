using Microsoft.Extensions.Logging;
using Ashlar.Abstractions;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Agents;

/// <summary>
/// Base class for specialized agents spawned from AgentSpawnSpec.
/// 
/// Provides common functionality for all agents:
/// - State management (Created, Initializing, Running, Completed, Failed)
/// - Health tracking (Healthy, Degraded, Unhealthy)
/// - Lifecycle hooks (Initialize, Execute, Cleanup)
/// - Timing and metrics
/// - Output management
/// 
/// Derived classes must implement ExecuteAsync to define agent-specific behavior.
/// The base class handles state transitions, error handling, and lifecycle management.
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

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseAgent"/> class.
    /// </summary>
    /// <param name="spec">The agent spawn specification defining the agent's behavior.</param>
    /// <param name="logger">The logger instance.</param>
    protected BaseAgent(AgentSpawnSpec spec, ILogger<BaseAgent> logger)
    {
        _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the agent's name (same as AgentId).
    /// </summary>
    public string Name => _spec.AgentId;

    /// <summary>
    /// Gets the agent spawn specification.
    /// </summary>
    public AgentSpawnSpec Spec => _spec;

    /// <summary>
    /// Gets or sets the agent's current state.
    /// 
    /// State transitions are logged and trigger OnStateChanged callbacks.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the agent's health status.
    /// 
    /// Health changes are logged and trigger OnHealthChanged callbacks.
    /// </summary>
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

    /// <summary>
    /// Gets the timestamp when the agent started execution.
    /// </summary>
    public DateTimeOffset? StartedAt => _startedAt;

    /// <summary>
    /// Gets the timestamp when the agent completed execution.
    /// </summary>
    public DateTimeOffset? CompletedAt => _completedAt;

    /// <summary>
    /// Gets the agent's execution output (available after completion).
    /// </summary>
    public object? Output => _output;

    /// <summary>
    /// Initializes the agent (loads resources, validates constraints).
    /// 
    /// Transitions agent from Created to Initializing, then to Running state.
    /// Derived classes can override to perform custom initialization logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown if agent is not in Created state</exception>
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
    /// Waits for dependencies to be resolved before execution.
    /// 
    /// Checks if all required dependencies (specified in AgentSpawnSpec) have outputs available.
    /// Transitions agent to Ready state once all dependencies are satisfied.
    /// </summary>
    /// <param name="dependencyOutputs">Dictionary of dependency agent IDs to their outputs</param>
    /// <param name="cancellationToken">Cancellation token</param>
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
    /// 
    /// Main execution method that:
    /// - Transitions agent to Running state
    /// - Calls abstract ExecuteCoreAsync (implemented by derived classes)
    /// - Handles errors and state transitions
    /// - Records execution timing
    /// - Stores output for downstream agents
    /// 
    /// Derived classes must implement ExecuteCoreAsync to define agent-specific behavior.
    /// </summary>
    /// <param name="dependencyOutputs">Outputs from dependency agents (if any)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent execution output</returns>
    /// <exception cref="InvalidOperationException">Thrown if agent is not in Ready state</exception>
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
    /// 
    /// Transitions agent to ShuttingDown state, calls OnShutdownAsync for cleanup,
    /// then transitions to Terminated state. If shutdown fails, agent is still terminated.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous shutdown operation.</returns>
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
    /// 
    /// This method is kept for IAgent compatibility but is not the primary execution path
    /// for orchestrated agents. Orchestrated agents use ExecuteAsync instead.
    /// </summary>
    /// <param name="obs">The agent observation (not used in orchestrated agents).</param>
    /// <param name="tools">The toolbox (not used in orchestrated agents).</param>
    /// <param name="mem">The agent memory (not used in orchestrated agents).</param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>AgentActions.None (orchestrated agents don't use this pattern).</returns>
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

    /// <summary>
    /// Called during agent initialization (after state transition to Initializing).
    /// 
    /// Derived classes should implement this to perform domain-specific initialization
    /// (e.g., loading resources, validating constraints, setting up connections).
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    protected abstract Task OnInitializeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Called when all dependencies have been resolved.
    /// 
    /// Derived classes can use this to prepare for execution using dependency outputs.
    /// This is called after WaitForDependenciesAsync confirms all dependencies are available.
    /// </summary>
    /// <param name="dependencyOutputs">Dictionary of dependency agent IDs to their outputs.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected abstract Task OnDependenciesResolvedAsync(
        IReadOnlyDictionary<string, object> dependencyOutputs,
        CancellationToken cancellationToken);

    /// <summary>
    /// Called to execute the agent's core task.
    /// 
    /// This is the main method that derived classes must implement to define agent behavior.
    /// The agent is in Executing state when this is called.
    /// </summary>
    /// <param name="dependencyOutputs">Outputs from dependency agents (if any).</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The agent's execution output (will be stored in Output property).</returns>
    protected abstract Task<object> OnExecuteAsync(
        IReadOnlyDictionary<string, object>? dependencyOutputs,
        CancellationToken cancellationToken);

    /// <summary>
    /// Called during agent shutdown (after state transition to ShuttingDown).
    /// 
    /// Derived classes should implement this to perform cleanup (e.g., releasing resources,
    /// closing connections, saving state).
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous shutdown operation.</returns>
    protected abstract Task OnShutdownAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Called when the agent's state changes.
    /// 
    /// Derived classes can override this to react to state transitions
    /// (e.g., logging, metrics, side effects).
    /// </summary>
    /// <param name="oldState">The previous state.</param>
    /// <param name="newState">The new state.</param>
    protected virtual void OnStateChanged(AgentState oldState, AgentState newState) { }

    /// <summary>
    /// Called when the agent's health changes.
    /// 
    /// Derived classes can override this to react to health changes
    /// (e.g., alerting, recovery actions).
    /// </summary>
    /// <param name="oldHealth">The previous health status.</param>
    /// <param name="newHealth">The new health status.</param>
    protected virtual void OnHealthChanged(AgentHealth oldHealth, AgentHealth newHealth) { }
}

