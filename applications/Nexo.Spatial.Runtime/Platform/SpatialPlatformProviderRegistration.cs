using Nexo.Spatial.Contracts;

namespace Nexo.Spatial.Runtime.Platform;

/// <summary>
/// Registered platform provider candidate for deterministic host selection.
/// </summary>
public sealed record SpatialPlatformProviderRegistration
{
    /// <summary>Stable platform identifier used for priority ordering.</summary>
    public string PlatformId { get; init; } = string.Empty;

    /// <summary>Predicate that reports whether this platform is supported on the current host.</summary>
    public Func<bool> IsSupported { get; init; } = static () => false;

    /// <summary>Factory that constructs the platform anchor provider instance.</summary>
    public Func<ISpatialAnchorProvider> Factory { get; init; } =
        static () => throw new InvalidOperationException("Factory not configured.");
}
