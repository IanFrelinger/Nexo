using Nexo.Core.Application.Scaling.Models;

namespace Nexo.Core.Application.Scaling.Ports;

/// <summary>
/// Supplies demand metrics (queue depth, pending tasks, etc.) used by autoscale loops.
/// Default is a no-op; mesh elastic status or custom collectors can replace it.
/// </summary>
public interface IWorkloadDemandSignal
{
    /// <summary>Reads the latest demand snapshot for a logical workload id.</summary>
    Task<WorkloadDemandSnapshot> GetDemandAsync(
        string workloadId,
        CancellationToken cancellationToken = default);
}
