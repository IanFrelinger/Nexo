using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents.Models;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.Agents;

/// <summary>
/// Monitors health and status of agents.
/// </summary>
public sealed class HealthMonitor
{
    private readonly ILogger<HealthMonitor> _logger;
    private readonly ConcurrentDictionary<string, AgentHealthStatus> _agentHealth = new();
    private readonly Timer? _healthCheckTimer;

    public HealthMonitor(ILogger<HealthMonitor> logger, TimeSpan? healthCheckInterval = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var interval = healthCheckInterval ?? TimeSpan.FromSeconds(30);
        _healthCheckTimer = new Timer(PerformHealthChecks, null, interval, interval);
    }

    /// <summary>
    /// Registers an agent for health monitoring.
    /// </summary>
    public void RegisterAgent(AgentContainer container)
    {
        _agentHealth[container.AgentId] = new AgentHealthStatus
        {
            AgentId = container.AgentId,
            Container = container,
            LastHealthCheck = DateTimeOffset.UtcNow,
            Health = container.Health,
            State = container.State
        };

        _logger.LogDebug("Registered agent {AgentId} for health monitoring", container.AgentId);
    }

    /// <summary>
    /// Unregisters an agent from health monitoring.
    /// </summary>
    public void UnregisterAgent(string agentId)
    {
        _agentHealth.TryRemove(agentId, out _);
        _logger.LogDebug("Unregistered agent {AgentId} from health monitoring", agentId);
    }

    /// <summary>
    /// Gets the health status of an agent.
    /// </summary>
    public AgentHealthStatus? GetHealthStatus(string agentId)
    {
        return _agentHealth.TryGetValue(agentId, out var status) ? status : null;
    }

    /// <summary>
    /// Gets health status of all registered agents.
    /// </summary>
    public IReadOnlyList<AgentHealthStatus> GetAllHealthStatuses()
    {
        return _agentHealth.Values.ToList();
    }

    /// <summary>
    /// Gets agents with unhealthy status.
    /// </summary>
    public IReadOnlyList<AgentHealthStatus> GetUnhealthyAgents()
    {
        return _agentHealth.Values
            .Where(s => s.Health == AgentHealth.Unhealthy)
            .ToList();
    }

    private void PerformHealthChecks(object? state)
    {
        foreach (var (agentId, healthStatus) in _agentHealth)
        {
            try
            {
                var container = healthStatus.Container;
                var currentHealth = container.Health;
                var currentState = container.State;

                // Update health status
                healthStatus.Health = currentHealth;
                healthStatus.State = currentState;
                healthStatus.LastHealthCheck = DateTimeOffset.UtcNow;

                // Check for state issues
                if (currentState == AgentState.Failed)
                {
                    _logger.LogWarning("Agent {AgentId} is in Failed state", agentId);
                }

                // Check for stuck states
                if (currentState == AgentState.Executing)
                {
                    var resourceUsage = container.GetResourceUsage();
                    if (resourceUsage.EstimatedComputeSeconds.HasValue &&
                        resourceUsage.Duration.TotalSeconds > resourceUsage.EstimatedComputeSeconds.Value * 2)
                    {
                        _logger.LogWarning("Agent {AgentId} has exceeded estimated compute time", agentId);
                        healthStatus.Health = AgentHealth.Degraded;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health for agent {AgentId}", agentId);
            }
        }
    }

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
    }
}

/// <summary>
/// Health status information for an agent.
/// </summary>
public sealed class AgentHealthStatus
{
    public required string AgentId { get; init; }
    public required AgentContainer Container { get; init; }
    public AgentHealth Health { get; set; }
    public AgentState State { get; set; }
    public DateTimeOffset LastHealthCheck { get; set; }
    public DateTimeOffset? LastStateChange { get; set; }
}

