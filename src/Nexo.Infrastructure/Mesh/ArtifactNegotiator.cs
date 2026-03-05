using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;

namespace Nexo.Infrastructure.Mesh;

/// <summary>
/// Negotiates artifact format between mesh instances by finding the best mutually supported format.
/// Considers environment flags: CanCompile → SourceCode, HasDockerRuntime → DockerImage, HasWasmRuntime → WasmModule.
/// Prefers: preferredFormat (if supported) > environment-based > fulfiller preferred > first common format.
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

        // Environment-based selection: requester (target) capabilities drive format preference
        if (requesterCapabilities.CanCompile && common.Contains(ArtifactFormat.Source))
            return ArtifactFormat.Source; // SourceCode equivalent
        if (requesterCapabilities.HasDockerRuntime && common.Contains(ArtifactFormat.DockerImage))
            return ArtifactFormat.DockerImage;
        if (requesterCapabilities.HasWasmRuntime && common.Contains(ArtifactFormat.WasmModule))
            return ArtifactFormat.WasmModule;

        if (fulfillerCapabilities.PreferredFormat.HasValue &&
            common.Contains(fulfillerCapabilities.PreferredFormat.Value))
            return fulfillerCapabilities.PreferredFormat;

        return common.First();
    }
}
