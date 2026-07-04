using Nexo.Core.Application.Mesh.Models;

namespace Nexo.Core.Application.Mesh.Ports;

/// <summary>
/// Request a capability from a peer.
/// </summary>
public interface ICapabilityRequester
{
    /// <summary>
    /// Requests a capability artifact from a peer using the preferred format.
    /// </summary>
    /// <param name="capability">Capability identifier to request.</param>
    /// <param name="preferredFormat">Preferred artifact format for the response.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Artifact from the fulfiller, or null when unavailable.</returns>
    Task<Artifact?> RequestAsync(string capability, ArtifactFormat preferredFormat, CancellationToken cancellationToken = default);
}
