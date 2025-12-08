using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents.Models;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Manages agent lifecycle: initialization, execution, and graceful shutdown.
/// </summary>
public sealed class LifecycleManager
{
    private readonly ILogger<LifecycleManager> _logger;
    private readonly Dictionary<string, AgentContainer> _activeAgents = new();
    private readonly HealthMonitor _healthMonitor;
    private readonly CancellationTokenSource _shutdownCts = new();

    public LifecycleManager(ILogger<LifecycleManager> logger, HealthMonitor healthMonitor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
    }

    /// <summary>
    /// Registers and initializes an agent.
    /// </summary>
    public async Task<AgentContainer> RegisterAgentAsync(
        AgentContainer container,
        CancellationToken cancellationToken = default)
    {
        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        _logger.LogInformation("Registering agent {AgentId}", container.AgentId);

        try
        {
            await container.InitializeAsync(cancellationToken);
            _activeAgents[container.AgentId] = container;
            _healthMonitor.RegisterAgent(container);
            
            _logger.LogInformation("Agent {AgentId} registered and initialized", container.AgentId);
            return container;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register agent {AgentId}", container.AgentId);
            throw;
        }
    }

    /// <summary>
    /// Executes an agent with dependency outputs.
    /// </summary>
    public async Task<object> ExecuteAgentAsync(
        string agentId,
        IReadOnlyDictionary<string, object>? dependencyOutputs = null,
        CancellationToken cancellationToken = default)
    {
        if (!_activeAgents.TryGetValue(agentId, out var container))
        {
            throw new InvalidOperationException($"Agent {agentId} is not registered");
        }

        _logger.LogInformation("Executing agent {AgentId}", agentId);

        try
        {
            var result = await container.ExecuteAsync(dependencyOutputs, cancellationToken);
            _logger.LogInformation("Agent {AgentId} completed execution", agentId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} execution failed", agentId);
            throw;
        }
    }

    /// <summary>
    /// Shuts down an agent gracefully.
    /// </summary>
    public async Task ShutdownAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (!_activeAgents.TryGetValue(agentId, out var container))
        {
            _logger.LogWarning("Attempted to shutdown unregistered agent {AgentId}", agentId);
            return;
        }

        _logger.LogInformation("Shutting down agent {AgentId}", agentId);

        try
        {
            await container.ShutdownAsync(cancellationToken);
            _activeAgents.Remove(agentId);
            _healthMonitor.UnregisterAgent(agentId);
            _logger.LogInformation("Agent {AgentId} shut down successfully", agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shutting down agent {AgentId}", agentId);
            // Force termination if graceful shutdown fails
            container.Terminate();
            _activeAgents.Remove(agentId);
            _healthMonitor.UnregisterAgent(agentId);
            throw;
        }
    }

    /// <summary>
    /// Shuts down all agents gracefully.
    /// </summary>
    public async Task ShutdownAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down all agents ({Count} agents)", _activeAgents.Count);

        var shutdownTasks = _activeAgents.Keys
            .Select(agentId => ShutdownAgentAsync(agentId, cancellationToken))
            .ToArray();

        await Task.WhenAll(shutdownTasks);

        _logger.LogInformation("All agents shut down");
    }

    /// <summary>
    /// Gets all active agents.
    /// </summary>
    public IReadOnlyList<AgentContainer> GetActiveAgents()
    {
        return _activeAgents.Values.ToList();
    }

    /// <summary>
    /// Gets an active agent by ID.
    /// </summary>
    public AgentContainer? GetAgent(string agentId)
    {
        return _activeAgents.TryGetValue(agentId, out var container) ? container : null;
    }

    /// <summary>
    /// Hot-reloads an agent definition (replaces existing agent with new spec).
    /// </summary>
    public async Task<AgentContainer> HotReloadAgentAsync(
        AgentContainer newContainer,
        CancellationToken cancellationToken = default)
    {
        if (newContainer == null)
        {
            throw new ArgumentNullException(nameof(newContainer));
        }

        var agentId = newContainer.AgentId;

        _logger.LogInformation("Hot-reloading agent {AgentId}", agentId);

        // Shutdown old agent if it exists
        if (_activeAgents.TryGetValue(agentId, out var oldContainer))
        {
            await ShutdownAgentAsync(agentId, cancellationToken);
        }

        // Register and initialize new agent
        return await RegisterAgentAsync(newContainer, cancellationToken);
    }
}

