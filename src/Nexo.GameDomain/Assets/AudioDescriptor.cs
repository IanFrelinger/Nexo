namespace Nexo.GameDomain.Assets;

/// <summary>
/// Data-only descriptor for a Unity audio clip's runtime settings.
/// </summary>
public sealed record AudioDescriptor
{
    /// <summary>Stable identifier for this audio asset.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Audio category for mixer routing.
    /// Accepted values: <c>"sfx"</c>, <c>"music"</c>, <c>"ambient"</c>, <c>"ui"</c>.
    /// </summary>
    public string Category { get; init; } = "sfx";

    /// <summary>Playback volume in the range [0, 1].</summary>
    public double Volume { get; init; } = 1.0;

    /// <summary>Pitch multiplier (1.0 = normal speed).</summary>
    public double Pitch { get; init; } = 1.0;

    /// <summary>Spatial blend where 0 = fully 2D and 1 = fully 3D.</summary>
    public double SpatialBlend { get; init; }

    /// <summary>Minimum distance for 3D audio attenuation.</summary>
    public double MinDistance { get; init; } = 1.0;

    /// <summary>Maximum distance for 3D audio attenuation.</summary>
    public double MaxDistance { get; init; } = 500.0;

    /// <summary>Whether the clip should loop.</summary>
    public bool Loop { get; init; }
}
