using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;

namespace Nexo.Infrastructure.Mesh;

/// <summary>
/// Negotiates artifact format between mesh instances by finding the best mutually supported format.
/// Prefers: preferredFormat (if supported by both) > fulfiller preferred > first common format.
/// </summary>
public sealed class ArtifactNegotiator : IArtifactNegotiator
{
    /// <inheritdoc />
    public ArtifactFormat? Negotiate(
        InstanceCapabilities requesterCapabilities,
        InstanceCapabilities fulfillerCapabilities,
        ArtifactFormat? preferredFormat = null)
    {
        var common = requesterCapabilities.SupportedFormats
            .Intersect(fulfillerCapabilities.SupportedFormats)
            .ToHashSet();

        if (common.Count == 0)
            return null;

        if (preferredFormat.HasValue && common.Contains(preferredFormat.Value))
            return preferredFormat.Value;

        if (fulfillerCapabilities.PreferredFormat.HasValue &&
            common.Contains(fulfillerCapabilities.PreferredFormat.Value))
            return fulfillerCapabilities.PreferredFormat;

        return common.First();
    }
}
