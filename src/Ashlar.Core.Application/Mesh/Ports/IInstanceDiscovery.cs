using Ashlar.Core.Application.Mesh.Models;

namespace Ashlar.Core.Application.Mesh.Ports;

/// <summary>
/// Discovers peer instances (e.g. via shared file ~/.ashlar/instances.json).
/// </summary>
public interface IInstanceDiscovery
{
    /// <summary>
    /// Discovers known peer instances from the configured discovery source.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>List of discovered peers with capabilities and trust tier.</returns>
    Task<IReadOnlyList<PeerInfo>> DiscoverAsync(CancellationToken cancellationToken = default);
}
