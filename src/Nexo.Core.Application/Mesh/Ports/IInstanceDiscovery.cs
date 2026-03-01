using Nexo.Core.Application.Mesh.Models;

namespace Nexo.Core.Application.Mesh.Ports;

/// <summary>
/// Discovers peer instances (e.g. via shared file ~/.nexo/instances.json).
/// </summary>
public interface IInstanceDiscovery
{
    Task<IReadOnlyList<PeerInfo>> DiscoverAsync(CancellationToken cancellationToken = default);
}
