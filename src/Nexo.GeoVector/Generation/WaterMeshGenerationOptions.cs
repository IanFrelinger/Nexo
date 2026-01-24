namespace Nexo.GeoVector.Generation;

public sealed record WaterMeshGenerationOptions
{
    public bool ConformToTerrain { get; init; }
    public bool TreatNoDataAsZero { get; init; } = true;

    /// <summary>
    /// If true and terrain is provided, flatten the entire polygon to a single sampled height
    /// (centroid-based) instead of per-vertex conformance.
    /// </summary>
    public bool FlattenToTerrain { get; init; }

    /// <summary>
    /// When conforming to terrain, offset the surface by this amount.
    /// </summary>
    public float SurfaceOffsetMeters { get; init; }

    public bool GenerateTexCoords { get; init; } = true;
    public float UvMetersPerRepeat { get; init; } = 5.0f;
}

