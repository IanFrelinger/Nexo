using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Agents;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.Health;

/// <summary>
/// Health check service for monitoring agent and system health.
/// 
/// Responsibilities:
/// - Registers and manages health checks
/// - Performs health checks with caching
/// - Provides health reports for all components
/// - Tracks agent health via AgentHealthCheck
/// 
/// Thread-safe implementation using concurrent collections.
/// Used by orchestration system to monitor component health.
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
