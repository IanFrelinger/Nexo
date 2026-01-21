namespace Nexo.GeoTerrain;

/// <summary>
/// Options for turning an <see cref="ElevationGrid"/> into a <see cref="MeshData"/>.
/// </summary>
public sealed record MeshGenerationOptions
{
    /// <summary>
    /// Vertical scale multiplier applied to elevation meters.
    /// </summary>
    public float VerticalScale { get; init; } = 1f;

    /// <summary>
    /// When true, compute per-vertex normals from triangle faces.
    /// </summary>
    public bool GenerateNormals { get; init; } = true;

    /// <summary>
    /// If true, NaN samples are clamped to 0m instead of failing.
    /// </summary>
    public bool TreatNoDataAsZero { get; init; }
}

