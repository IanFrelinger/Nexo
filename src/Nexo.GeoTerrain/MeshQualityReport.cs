namespace Nexo.GeoTerrain;

/// <summary>
/// Basic, deterministic mesh quality metrics.
/// </summary>
public sealed record MeshQualityReport
{
    public required int VertexCount { get; init; }
    public required int TriangleCount { get; init; }
    public required float MinHeightMeters { get; init; }
    public required float MaxHeightMeters { get; init; }
    public required int NoDataSamples { get; init; }
}

