using Nexo.Core.Application.Mesh.Models;

namespace Nexo.Core.Application.Mesh.Ports;

/// <summary>
/// Request a capability from a peer.
/// </summary>
public interface ICapabilityRequester
{
    Task<Artifact?> RequestAsync(string capability, ArtifactFormat preferredFormat, CancellationToken cancellationToken = default);
}
