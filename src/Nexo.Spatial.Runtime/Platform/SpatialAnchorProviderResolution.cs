using Nexo.Spatial.Contracts;

namespace Nexo.Spatial.Runtime.Platform;

/// <summary>
/// Result of host platform provider resolution. Callers must distinguish
/// <see cref="IsUnavailable"/> (no tracking surface) from downstream <see cref="TrackingState.Lost"/> samples.
/// </summary>
public sealed record SpatialAnchorProviderResolution
{
    public ISpatialAnchorProvider? Provider { get; init; }

    public SpatialAnchorProviderUnavailableReason? UnavailableReason { get; init; }

    public string? SelectedPlatformId { get; init; }

    public bool IsUnavailable => Provider is null;

    public static SpatialAnchorProviderResolution Available(ISpatialAnchorProvider provider, string platformId) =>
        new() { Provider = provider, SelectedPlatformId = platformId };

    public static SpatialAnchorProviderResolution Unavailable(
        SpatialAnchorProviderUnavailableReason reason,
        string? platformId = null) =>
        new() { UnavailableReason = reason, SelectedPlatformId = platformId };
}
