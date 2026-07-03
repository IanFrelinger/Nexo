using Nexo.Spatial.Contracts;

namespace Nexo.Spatial.Runtime.Platform;

/// <summary>
/// Registered platform provider candidate for deterministic host selection.
/// </summary>
public sealed record SpatialPlatformProviderRegistration
{
    public string PlatformId { get; init; } = string.Empty;

    public Func<bool> IsSupported { get; init; } = static () => false;

    public Func<ISpatialAnchorProvider> Factory { get; init; } =
        static () => throw new InvalidOperationException("Factory not configured.");
}
