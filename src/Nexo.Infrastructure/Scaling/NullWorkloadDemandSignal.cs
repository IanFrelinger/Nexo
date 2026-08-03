using Nexo.Core.Application.Scaling.Models;
using Nexo.Core.Application.Scaling.Ports;

namespace Nexo.Infrastructure.Scaling;

/// <summary>Default demand signal with zero queue pressure.</summary>
public sealed class NullWorkloadDemandSignal : IWorkloadDemandSignal
{
    /// <inheritdoc />
    public Task<WorkloadDemandSnapshot> GetDemandAsync(
        string workloadId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new WorkloadDemandSnapshot(
            workloadId,
            QueueDepth: 0,
            PendingTasks: 0,
            ObservedAtUtc: DateTimeOffset.UtcNow));
}
