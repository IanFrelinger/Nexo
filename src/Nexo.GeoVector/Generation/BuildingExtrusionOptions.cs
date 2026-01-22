namespace Nexo.GeoVector.Generation;

/// <summary>
/// Options for extruding building footprints into a 3D mesh.
/// </summary>
public sealed class BuildingExtrusionOptions
{
    public float DefaultHeightMeters { get; init; } = 10.0f;
    public float MinHeightMeters { get; init; } = 1.0f;
    public bool IncludeBottom { get; init; }

    /// <summary>
    /// If true, generates per-vertex UVs in meters-based space (supports consistent texture scale).
    /// </summary>
    public bool GenerateTexCoords { get; init; }

    /// <summary>
    /// Meters per texture repeat (1.0 => 1 UV unit per meter).
    /// </summary>
    public float UvMetersPerRepeat { get; init; } = 1.0f;
}

