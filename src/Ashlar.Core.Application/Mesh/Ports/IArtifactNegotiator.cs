using Ashlar.Core.Application.Mesh.Models;

namespace Ashlar.Core.Application.Mesh.Ports;

/// <summary>
/// Negotiates artifact format between mesh instances.
/// Finds the best mutually supported format given requester and fulfiller capabilities.
/// </summary>
public interface IArtifactNegotiator
{
    /// <summary>
    /// Negotiates the format to use for a capability transfer.
    /// </summary>
    /// <param name="requesterCapabilities">What the requester can consume.</param>
    /// <param name="fulfillerCapabilities">What the fulfiller can produce.</param>
    /// <param name="preferredFormat">Optional preferred format from the requester.</param>
    /// <returns>The negotiated format, or null if no common format exists.</returns>
    ArtifactFormat? Negotiate(
        InstanceCapabilities requesterCapabilities,
        InstanceCapabilities fulfillerCapabilities,
        ArtifactFormat? preferredFormat = null);
}
