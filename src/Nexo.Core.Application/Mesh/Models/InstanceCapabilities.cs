namespace Nexo.Core.Application.Mesh.Models;

/// <summary>
/// Describes the artifact formats an instance can produce or consume.
/// Used by <see cref="Ports.IArtifactNegotiator"/> to negotiate format between mesh instances.
/// </summary>
public sealed class InstanceCapabilities
{
    /// <summary>
    /// Formats this instance can produce (e.g. when fulfilling requests).
    /// </summary>
    public IReadOnlyCollection<ArtifactFormat> SupportedFormats { get; }

    /// <summary>
    /// Preferred format when multiple are available.
    /// </summary>
    public ArtifactFormat? PreferredFormat { get; }

    public InstanceCapabilities(
        IEnumerable<ArtifactFormat> supportedFormats,
        ArtifactFormat? preferredFormat = null)
    {
        SupportedFormats = new List<ArtifactFormat>(new HashSet<ArtifactFormat>(supportedFormats)).AsReadOnly();
        PreferredFormat = preferredFormat;
    }

    /// <summary>
    /// Capabilities that support all standard formats (Source, Binary, Config).
    /// </summary>
    public static InstanceCapabilities AllFormats { get; } = new(
        new[] { ArtifactFormat.Source, ArtifactFormat.Binary, ArtifactFormat.Config },
        ArtifactFormat.Source);
}
