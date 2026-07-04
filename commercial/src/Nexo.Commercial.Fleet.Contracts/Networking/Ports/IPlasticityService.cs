using Nexo.Commercial.Fleet.Contracts.Networking.Models;

namespace Nexo.Commercial.Fleet.Contracts.Networking.Ports;
/// <summary>
/// Orchestrates plasticity metrics and optional periodic tasks (usage reporting, directory refresh).
/// </summary>
public interface IPlasticityService
{
    /// <summary>Get aggregated plasticity metrics from usage tracker, cache, network bus, knowledge sync, agent directory.</summary>
    Task<PlasticityMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);
}
