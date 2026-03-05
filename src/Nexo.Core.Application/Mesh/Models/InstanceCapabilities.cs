namespace Nexo.Core.Application.Mesh.Models;

/// <summary>
/// Describes the artifact formats an instance can produce or consume,
/// and environment flags for mesh negotiation.
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

    /// <summary>Whether this instance can compile source code.</summary>
    public bool CanCompile { get; }

    /// <summary>Whether this instance has a Docker runtime.</summary>
    public bool HasDockerRuntime { get; }

    /// <summary>Whether this instance has a WebAssembly runtime.</summary>
    public bool HasWasmRuntime { get; }

    /// <summary>Whether this instance is air-gapped (no network).</summary>
    public bool IsAirGapped { get; }

    /// <summary>Available component IDs (e.g. nexo-cli, nexo-observe).</summary>
    public IReadOnlyCollection<string> AvailableComponents { get; }

    public InstanceCapabilities(
        IEnumerable<ArtifactFormat> supportedFormats,
        ArtifactFormat? preferredFormat = null,
        bool canCompile = false,
        bool hasDockerRuntime = false,
        bool hasWasmRuntime = false,
        bool isAirGapped = false,
        IEnumerable<string>? availableComponents = null)
    {
        SupportedFormats = new List<ArtifactFormat>(new HashSet<ArtifactFormat>(supportedFormats)).AsReadOnly();
        PreferredFormat = preferredFormat;
        CanCompile = canCompile;
        HasDockerRuntime = hasDockerRuntime;
        HasWasmRuntime = hasWasmRuntime;
        IsAirGapped = isAirGapped;
        AvailableComponents = availableComponents != null
            ? new List<string>(availableComponents).AsReadOnly()
            : Array.Empty<string>();
    }

    /// <summary>
    /// Capabilities that support all standard formats (Source, Binary, Config).
    /// </summary>
    public static InstanceCapabilities AllFormats { get; } = new(
        new[] { ArtifactFormat.Source, ArtifactFormat.Binary, ArtifactFormat.Config },
        ArtifactFormat.Source);

    /// <summary>
    /// Local Nexo instance capabilities: can compile, supports Source/Binary/Config.
    /// </summary>
    public static InstanceCapabilities LocalNexo { get; } = new(
        new[] { ArtifactFormat.Source, ArtifactFormat.Binary, ArtifactFormat.Config },
        ArtifactFormat.Source,
        canCompile: true,
        hasDockerRuntime: false,
        hasWasmRuntime: false,
        isAirGapped: false,
        availableComponents: new[] { "nexo-cli", "nexo-observe", "nexo-improve" });
}
