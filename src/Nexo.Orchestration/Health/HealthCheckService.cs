using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.Health;

/// <summary>
/// Health check service for monitoring agent and system health.
/// </summary>
public sealed class HealthCheckService
{
    private readonly ILogger<HealthCheckService>? _logger;
    private readonly ConcurrentDictionary<string, IHealthCheck> _healthChecks = new();
    private readonly ConcurrentDictionary<string, HealthStatus> _statusCache = new();
    private readonly TimeSpan _cacheTimeout;

    public HealthCheckService(
        TimeSpan? cacheTimeout = null,
        ILogger<HealthCheckService>? logger = null)
    {
        _cacheTimeout = cacheTimeout ?? TimeSpan.FromSeconds(30);
        _logger = logger;
    }

    /// <summary>
    /// Registers a health check.
    /// </summary>
    public void Register(string name, IHealthCheck healthCheck)
    {
        _healthChecks[name] = healthCheck;
        _logger?.LogDebug("Registered health check: {Name}", name);
    }

    /// <summary>
    /// Checks the health of a specific component.
    /// </summary>
    public async Task<HealthStatus> CheckHealthAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_healthChecks.TryGetValue(name, out var healthCheck))
        {
            return new HealthStatus
            {
                Name = name,
                IsHealthy = false,
                Status = "Unknown",
                Message = $"Health check '{name}' not found",
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        // Check cache
        if (_statusCache.TryGetValue(name, out var cachedStatus))
        {
            if (DateTimeOffset.UtcNow - cachedStatus.Timestamp < _cacheTimeout)
            {
                return cachedStatus;
            }
        }

        try
        {
            var status = await healthCheck.CheckAsync(cancellationToken);
            _statusCache[name] = status;
            return status;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Health check {Name} failed", name);
            var errorStatus = new HealthStatus
            {
                Name = name,
                IsHealthy = false,
                Status = "Error",
                Message = ex.Message,
                Timestamp = DateTimeOffset.UtcNow,
                Details = new Dictionary<string, object> { ["exception"] = ex.ToString() }
            };
            _statusCache[name] = errorStatus;
            return errorStatus;
        }
    }

    /// <summary>
    /// Checks health of all registered components.
    /// </summary>
    public async Task<HealthReport> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new List<HealthStatus>();

        foreach (var (name, _) in _healthChecks)
        {
            var status = await CheckHealthAsync(name, cancellationToken);
            statuses.Add(status);
        }

        var overallHealthy = statuses.All(s => s.IsHealthy);

        return new HealthReport
        {
            OverallHealthy = overallHealthy,
            Timestamp = DateTimeOffset.UtcNow,
            Statuses = statuses
        };
    }

    /// <summary>
    /// Clears the health status cache.
    /// </summary>
    public void ClearCache()
    {
        _statusCache.Clear();
        _logger?.LogDebug("Cleared health status cache");
    }
}

/// <summary>
/// Interface for health checks.
/// </summary>
public interface IHealthCheck
{
    Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Health status of a component.
/// </summary>
public sealed record HealthStatus
{
    public required string Name { get; init; }
    public required bool IsHealthy { get; init; }
    public required string Status { get; init; }
    public string? Message { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public IReadOnlyDictionary<string, object> Details { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Overall health report.
/// </summary>
public sealed record HealthReport
{
    public required bool OverallHealthy { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required IReadOnlyList<HealthStatus> Statuses { get; init; }
}

/// <summary>
/// Health check for an agent.
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
        var isHealthy = _agent.State != Agents.Models.AgentState.Failed &&
                        _agent.Health != Agents.Models.AgentHealth.Unhealthy;

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

