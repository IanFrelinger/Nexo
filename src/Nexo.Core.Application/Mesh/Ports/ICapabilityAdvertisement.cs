using Nexo.Core.Application.Mesh.Models;

namespace Nexo.Core.Application.Mesh.Ports;

/// <summary>
/// Advertise capabilities and find peers.
/// </summary>
public interface ICapabilityAdvertisement
{
    Task AdvertiseAsync(IReadOnlyList<CapabilityDescriptor> capabilities, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PeerInfo>> FindPeersWithCapabilityAsync(string capability, CancellationToken cancellationToken = default);
}
