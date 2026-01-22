using Nexo.GeoTerrain;

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

    /// <summary>
    /// If true, samples terrain elevation and offsets building base heights.
    /// </summary>
    public bool AlignToTerrain { get; init; }

    /// <summary>
    /// Optional terrain grid used for alignment. When null, alignment is skipped.
    /// </summary>
    public ElevationGrid? TerrainGrid { get; init; }

    /// <summary>
    /// If true, NoData/NaN terrain samples are treated as 0m; otherwise they fall back to 0m only when sampling fails.
    /// </summary>
    public bool TerrainTreatNoDataAsZero { get; init; }
}

