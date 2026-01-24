namespace Nexo.GeoTerrain;

/// <summary>
/// Comprehensive mesh quality metrics including triangle quality, accuracy, and validation.
/// </summary>
public sealed record MeshQualityReport
{
    // Basic metrics
    public required int VertexCount { get; init; }
    public required int TriangleCount { get; init; }
    public required float MinHeightMeters { get; init; }
    public required float MaxHeightMeters { get; init; }
    public required int NoDataSamples { get; init; }

    // Triangle quality metrics
    public float? MinAspectRatio { get; init; }
    public float? MaxAspectRatio { get; init; }
    public float? AverageAspectRatio { get; init; }
    public int? ThinTriangleCount { get; init; } // Aspect ratio < 0.1
    public float? MinTriangleArea { get; init; }
    public float? MaxTriangleArea { get; init; }
    public float? AverageTriangleArea { get; init; }
    public float? EdgeLengthVariance { get; init; }

    // Mesh accuracy metrics (compared to source elevation grid)
    public float? MaxDeviationMeters { get; init; }
    public float? RmsErrorMeters { get; init; }
    public float? MeanErrorMeters { get; init; }

    // Slope validation
    public float? MaxSlopeDegrees { get; init; }
    public int? SteepSlopeCount { get; init; } // Slopes > 45 degrees
}

