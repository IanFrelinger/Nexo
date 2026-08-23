using Ashlar.Spatial.Contracts;

namespace Ashlar.Spatial.Runtime.Platform;

/// <summary>
/// Result of host platform provider resolution. Callers must distinguish
/// <see cref="IsUnavailable"/> (no tracking surface) from downstream <see cref="TrackingState.Lost"/> samples.
/// </summary>
public sealed record SpatialAnchorProviderResolution
{
    /// <summary>Resolved anchor provider when available; otherwise <see langword="null"/>.</summary>
    public ISpatialAnchorProvider? Provider { get; init; }

    /// <summary>Reason the provider could not be resolved, when <see cref="IsUnavailable"/> is true.</summary>
    public SpatialAnchorProviderUnavailableReason? UnavailableReason { get; init; }

    /// <summary>Platform identifier of the selected or attempted provider.</summary>
    public string? SelectedPlatformId { get; init; }

    /// <summary>Whether no anchor provider could be resolved for the host.</summary>
    public bool IsUnavailable => Provider is null;

    /// <summary>Creates a successful resolution for the given provider and platform id.</summary>
    public static SpatialAnchorProviderResolution Available(ISpatialAnchorProvider provider, string platformId) =>
        new() { Provider = provider, SelectedPlatformId = platformId };

    /// <summary>Creates an unavailable resolution with an explicit reason.</summary>
    public static SpatialAnchorProviderResolution Unavailable(
        SpatialAnchorProviderUnavailableReason reason,
        string? platformId = null) =>
        new() { UnavailableReason = reason, SelectedPlatformId = platformId };
}
