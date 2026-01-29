using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Logging;
using Nexo.BackgroundAgents.Scheduling;

namespace Nexo.BackgroundAgents.Registry;

/// <summary>
/// Registry for managing background agent instances.
///
/// Provides:
/// - Agent registration and lifecycle management
/// - State tracking
/// - Execution coordination
/// - Agent lookup
///
/// Thread-safe implementation using concurrent collections.
/// </summary>
public interface IBackgroundAgentRegistry
{
    /// <summary>
    /// Register a background agent.
    /// </summary>
    /// <param name="agent">The agent instance.</param>
    /// <param name="config">The agent configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RegisterAsync(IAgent agent, BackgroundAgentConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an agent instance by ID.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <returns>The agent instance, or null if not found.</returns>
    BackgroundAgentInstance? GetAgent(string agentId);

    /// <summary>
    /// Get all registered agents.
    /// </summary>
    /// <returns>All registered agent instances.</returns>
    IReadOnlyList<BackgroundAgentInstance> GetAll();

    /// <summary>
    /// Start all registered agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop all registered agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a specific agent.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StartAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop a specific agent.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run one execution cycle for an agent (for manual/testing use).
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteOnceAsync(string agentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of IBackgroundAgentRegistry.
/// </summary>
public sealed class BackgroundAgentRegistry : IBackgroundAgentRegistry
{
    private readonly ConcurrentDictionary<string, BackgroundAgentInstance> _agents = new();
    private readonly IAgentScheduler _scheduler;
    private readonly ILogger<BackgroundAgentRegistry>? _logger;
    private readonly IBackgroundAgentLogStore? _logStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundAgentRegistry"/> class.
    /// Agent creation is done by the host (e.g. BackgroundAgentService) before RegisterAsync.
    /// </summary>
    /// <param name="scheduler">Scheduler for agent execution loops.</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="logStore">Optional log store for agent execution logs.</param>
    public BackgroundAgentRegistry(
        IAgentScheduler scheduler,
        ILogger<BackgroundAgentRegistry>? logger = null,
        IBackgroundAgentLogStore? logStore = null)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _logger = logger;
        _logStore = logStore;
    }

    /// <summary>
    /// Register a background agent.
    /// </summary>
    public Task RegisterAsync(IAgent agent, BackgroundAgentConfig config, CancellationToken cancellationToken = default)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        // Create agent instance
        var instance = new BackgroundAgentInstance
        {
            Agent = agent,
            Config = config,
            State = BackgroundAgentState.Idle
        };

        _agents[config.Id] = instance;
        _logger?.LogInformation("Registered background agent: {AgentId}", config.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get an agent instance by ID.
    /// </summary>
    public BackgroundAgentInstance? GetAgent(string agentId)
    {
        return _agents.TryGetValue(agentId, out var instance) ? instance : null;
    }

    /// <summary>
    /// Get all registered agents.
    /// </summary>
    public IReadOnlyList<BackgroundAgentInstance> GetAll()
    {
        return _agents.Values.ToList();
    }

    /// <summary>
    /// Start all registered agents.
    /// </summary>
    public Task StartAllAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _agents.Values
            .Where(a => a.Config.Enabled)
            .Select(a => StartAsync(a.Config.Id, cancellationToken));
        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// Stop all registered agents.
    /// </summary>
    public Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _agents.Values.Select(a => StopAsync(a.Config.Id, cancellationToken));
        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// Start a specific agent.
    /// </summary>
    public Task StartAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (!_agents.TryGetValue(agentId, out var instance))
        {
            throw new InvalidOperationException($"Agent {agentId} not found");
        }

        if (instance.State == BackgroundAgentState.Running)
        {
            _logger?.LogWarning("Agent {AgentId} is already running", agentId);
            return Task.CompletedTask;
        }

        instance.State = BackgroundAgentState.Starting;
        instance.LastStartedAt = DateTimeOffset.UtcNow;

        _scheduler.StartAsync(instance, ExecuteAgentAsync, cancellationToken);

        instance.State = BackgroundAgentState.Running;
        _logger?.LogInformation("Started background agent: {AgentId}", agentId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop a specific agent.
    /// </summary>
    public Task StopAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (!_agents.TryGetValue(agentId, out var instance))
        {
            throw new InvalidOperationException($"Agent {agentId} not found");
        }

        if (instance.State == BackgroundAgentState.Stopped || instance.State == BackgroundAgentState.Idle)
        {
            return Task.CompletedTask;
        }

        instance.State = BackgroundAgentState.Stopping;

        _scheduler.Stop(agentId);

        instance.State = BackgroundAgentState.Stopped;
        _logger?.LogInformation("Stopped background agent: {AgentId}", agentId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ExecuteOnceAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (!_agents.TryGetValue(agentId, out var instance))
            throw new InvalidOperationException($"Agent {agentId} not found");
        await ExecuteAgentAsync(instance, cancellationToken).ConfigureAwait(false);
    }

    private Task ExecuteAgentAsync(BackgroundAgentInstance instance, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var agentId = instance.Config.Id;
        try
        {
            instance.ExecutionCount++;
            _logStore?.Append(agentId, "Info", "Executing background agent.");
            _logger?.LogDebug("Executing background agent: {AgentId}", agentId);

            // Create a simple observation for the agent
            var observation = new AgentObservation(new WorldSnapshot(0, new Dictionary<string, object?>
            {
                ["agentId"] = agentId,
                ["timestamp"] = DateTimeOffset.UtcNow
            }));

            // Execute agent (this would need a toolbox and memory - simplified for now).
            // When integrating: use BackgroundAgentPolicyEngineFactory.Create(registry, sensitivityRegistry)
            // as the PolicyEngine for the host so tool calls are enforced by DataExfiltrationPolicy.
            // var actions = await instance.Agent.ThinkAsync(observation, toolbox, memory, cancellationToken);

            instance.LastCompletedAt = DateTimeOffset.UtcNow;
            instance.SuccessCount++;
            _logStore?.Append(agentId, "Info", "Execution completed successfully.");
            _logger?.LogDebug("Background agent {AgentId} executed successfully", agentId);
            return Task.CompletedTask;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            instance.FailureCount++;
            instance.LastError = ex.Message;
            _logStore?.Append(agentId, "Error", $"Execution failed: {ex.Message}");
            _logger?.LogError(ex, "Background agent {AgentId} execution failed", agentId);
            return Task.CompletedTask;
        }
    }
}
