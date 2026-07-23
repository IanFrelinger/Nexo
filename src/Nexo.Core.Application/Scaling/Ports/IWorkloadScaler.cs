using Nexo.Core.Application.Scaling.Models;

namespace Nexo.Core.Application.Scaling.Ports;

/// <summary>
/// First-class port for dynamically scaling containerized workloads.
/// Implementations may target Kubernetes Deployments, Docker Compose services,
/// ECS, Nomad, etc. Hosts resolve a single active provider via DI.
/// </summary>
public interface IWorkloadScaler
{
    /// <summary>Stable provider id (e.g. <c>kubernetes</c>, <c>compose</c>, <c>null</c>).</summary>
    string ProviderName { get; }

    /// <summary>True when the underlying control plane is reachable for this provider.</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists configured logical workloads this scaler can manage.</summary>
    Task<IReadOnlyList<WorkloadDescriptor>> ListWorkloadsAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads current / desired / ready replica counts for a workload.</summary>
    Task<WorkloadReplicaSnapshot> GetReplicasAsync(
        string workloadId,
        CancellationToken cancellationToken = default);

    /// <summary>Sets the desired replica count for a workload (clamped by provider policy/options).</summary>
    Task<WorkloadScaleResult> ScaleAsync(
        WorkloadScaleRequest request,
        CancellationToken cancellationToken = default);
}
