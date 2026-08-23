using Ashlar.Core.Application.Mesh.Models;

namespace Ashlar.Core.Application.Mesh.Ports;

/// <summary>
/// Advertise capabilities and find peers.
/// </summary>
public interface ICapabilityAdvertisement
{
    /// <summary>
    /// Publishes capability descriptors for this instance.
    /// </summary>
    /// <param name="capabilities">Capabilities to advertise.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task AdvertiseAsync(IReadOnlyList<CapabilityDescriptor> capabilities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds peers that advertise the given capability.
    /// </summary>
    /// <param name="capability">Capability identifier to search for.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Peers that can fulfill the capability.</returns>
    Task<IReadOnlyList<PeerInfo>> FindPeersWithCapabilityAsync(string capability, CancellationToken cancellationToken = default);
}
