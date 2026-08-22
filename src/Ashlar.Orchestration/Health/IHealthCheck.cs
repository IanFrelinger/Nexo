using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Agents;
using System.Collections.Concurrent;

namespace Ashlar.Orchestration.Health;

/// <summary>
/// Interface for health checks.
/// 
/// Defines the contract for health check implementations:
/// - CheckAsync method that returns HealthStatus
/// 
/// Implementations (AgentHealthCheck, etc.) provide specific health checking logic.
/// Used by HealthCheckService to monitor component health.
/// </summary>
public interface IHealthCheck
{
    Task<HealthStatus> CheckAsync(CancellationToken cancellationToken = default);
}
