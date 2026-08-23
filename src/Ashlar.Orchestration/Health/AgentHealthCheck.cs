using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Agents;
using System.Collections.Concurrent;

namespace Ashlar.Orchestration.Health;

/// <summary>
/// Health check for an agent.
/// 
/// Responsibilities:
/// - Checks agent state and health status
/// - Returns HealthStatus based on agent state
/// - Includes agent details in health status
/// 
/// Implements IHealthCheck for agent health monitoring.
/// Used by HealthCheckService to monitor agent health.
/// </summary>
public sealed class AgentHealthCheck : IHealthCheck
{
    private readonly AgentContainer _agent;
    private readonly ILogger<AgentHealthCheck>? _logger;

    public AgentHealthCheck(AgentContainer agent, ILogger<AgentHealthCheck>? logger = null)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _logger = logger;
    }

    public Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        var isHealthy = _agent.State != AgentState.Failed &&
                        _agent.Health != AgentHealth.Unhealthy;

        var status = isHealthy ? "Healthy" : "Unhealthy";
        var message = $"Agent {_agent.AgentId} is {status}";

        if (!isHealthy)
        {
            message += $" (State: {_agent.State}, Health: {_agent.Health})";
        }

        return Task.FromResult(new HealthStatus
        {
            Name = _agent.AgentId,
            IsHealthy = isHealthy,
            Status = status,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["state"] = _agent.State.ToString(),
                ["health"] = _agent.Health.ToString(),
                ["startedAt"] = _agent.Agent.StartedAt?.ToString() ?? "null"
            }
        });
    }
}
