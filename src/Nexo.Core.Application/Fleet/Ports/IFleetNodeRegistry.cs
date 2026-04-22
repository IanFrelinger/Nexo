using Nexo.Core.Application.Fleet.Models;

namespace Nexo.Core.Application.Fleet.Ports;

/// <summary>
/// In-process registry of mesh worker nodes (Phase 1 control plane).
/// </summary>
public interface IFleetNodeRegistry
{
    Task RegisterOrUpdateAsync(MeshFleetNodeState node, CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(string peerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeshFleetNodeState>> ListAsync(CancellationToken cancellationToken = default);

    Task<MeshFleetNodeState?> GetAsync(string peerId, CancellationToken cancellationToken = default);

    Task<bool> SetDrainedAsync(string peerId, bool drained, CancellationToken cancellationToken = default);

    /// <param name="peerId">Worker peer id.</param>
    /// <param name="reportedQueueDepth">Optional worker-reported queue depth for elastic placement (Phase 5).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HeartbeatAsync(string peerId, int? reportedQueueDepth = null, CancellationToken cancellationToken = default);
}
