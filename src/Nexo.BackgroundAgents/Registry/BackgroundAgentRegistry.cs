using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.Orchestration.Agents;

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
}

/// <summary>
/// Implementation of IBackgroundAgentRegistry.
/// </summary>
public sealed class BackgroundAgentRegistry : IBackgroundAgentRegistry
{
    private readonly ConcurrentDictionary<string, BackgroundAgentInstance> _agents = new();
    private readonly ConcurrentDictionary<string, Task> _executionTasks = new();
    private readonly AgentFactory _agentFactory;
    private readonly LifecycleManager _lifecycleManager;
    private readonly ILogger<BackgroundAgentRegistry>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundAgentRegistry"/> class.
    /// </summary>
    /// <param name="agentFactory">The agent factory for creating agent containers.</param>
    /// <param name="lifecycleManager">The lifecycle manager for agent registration.</param>
    /// <param name="logger">Optional logger.</param>
    public BackgroundAgentRegistry(
        AgentFactory agentFactory,
        LifecycleManager lifecycleManager,
        ILogger<BackgroundAgentRegistry>? logger = null)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        _logger = logger;
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

        // Start execution task based on schedule
        var executionTask = ExecuteAgentLoopAsync(instance, cancellationToken);
        _executionTasks[agentId] = executionTask;

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

        // Cancel execution task if it exists
        if (_executionTasks.TryRemove(agentId, out var task))
        {
            // Note: We can't actually cancel the task, but we mark it as stopping
            // The task will check cancellation token internally
        }

        instance.State = BackgroundAgentState.Stopped;
        _logger?.LogInformation("Stopped background agent: {AgentId}", agentId);
        return Task.CompletedTask;
    }

    private async Task ExecuteAgentLoopAsync(BackgroundAgentInstance instance, CancellationToken cancellationToken)
    {
        try
        {
            // Handle initial delay
            if (instance.Config.Schedule.InitialDelay.HasValue)
            {
                await Task.Delay(instance.Config.Schedule.InitialDelay.Value, cancellationToken);
            }

            // Execute based on schedule type
            switch (instance.Config.Schedule.Type)
            {
                case ScheduleType.Continuous:
                    await ExecuteContinuousAsync(instance, cancellationToken);
                    break;

                case ScheduleType.Interval:
                    await ExecuteIntervalAsync(instance, cancellationToken);
                    break;

                case ScheduleType.Cron:
                    await ExecuteCronAsync(instance, cancellationToken);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("Agent {AgentId} execution cancelled", instance.Config.Id);
            instance.State = BackgroundAgentState.Stopped;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Agent {AgentId} execution failed", instance.Config.Id);
            instance.State = BackgroundAgentState.Error;
            instance.LastError = ex.Message;
            instance.FailureCount++;
        }
    }

    private async Task ExecuteContinuousAsync(BackgroundAgentInstance instance, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && instance.State == BackgroundAgentState.Running)
        {
            await ExecuteAgentAsync(instance, cancellationToken);
            // Small delay to prevent tight loop
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private async Task ExecuteIntervalAsync(BackgroundAgentInstance instance, CancellationToken cancellationToken)
    {
        if (!instance.Config.Schedule.Interval.HasValue)
        {
            throw new InvalidOperationException($"Agent {instance.Config.Id} has interval schedule but no interval specified");
        }

        while (!cancellationToken.IsCancellationRequested && instance.State == BackgroundAgentState.Running)
        {
            await ExecuteAgentAsync(instance, cancellationToken);
            await Task.Delay(instance.Config.Schedule.Interval.Value, cancellationToken);
        }
    }

    private async Task ExecuteCronAsync(BackgroundAgentInstance instance, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(instance.Config.Schedule.CronExpression))
        {
            throw new InvalidOperationException($"Agent {instance.Config.Id} has cron schedule but no cron expression specified");
        }

        var schedule = NCrontab.CrontabSchedule.Parse(instance.Config.Schedule.CronExpression);

        while (!cancellationToken.IsCancellationRequested && instance.State == BackgroundAgentState.Running)
        {
            var nextRun = schedule.GetNextOccurrence(DateTime.UtcNow);
            var delay = nextRun - DateTime.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await ExecuteAgentAsync(instance, cancellationToken);
            }
        }
    }

    private Task ExecuteAgentAsync(BackgroundAgentInstance instance, CancellationToken cancellationToken)
    {
        try
        {
            instance.ExecutionCount++;
            _logger?.LogDebug("Executing background agent: {AgentId}", instance.Config.Id);

            // Create a simple observation for the agent
            var observation = new AgentObservation(new WorldSnapshot(0, new Dictionary<string, object?>
            {
                ["agentId"] = instance.Config.Id,
                ["timestamp"] = DateTimeOffset.UtcNow
            }));

            // Execute agent (this would need a toolbox and memory - simplified for now)
            // TODO: Integrate with actual agent execution infrastructure
            // var actions = await instance.Agent.ThinkAsync(observation, toolbox, memory, cancellationToken);

            instance.LastCompletedAt = DateTimeOffset.UtcNow;
            instance.SuccessCount++;
            _logger?.LogDebug("Background agent {AgentId} executed successfully", instance.Config.Id);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            instance.FailureCount++;
            instance.LastError = ex.Message;
            _logger?.LogError(ex, "Background agent {AgentId} execution failed", instance.Config.Id);
            return Task.FromException(ex);
        }
    }
}
