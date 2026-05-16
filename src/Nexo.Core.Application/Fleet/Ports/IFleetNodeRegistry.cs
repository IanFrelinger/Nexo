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

    Task HeartbeatAsync(string peerId, CancellationToken cancellationToken = default);
}
