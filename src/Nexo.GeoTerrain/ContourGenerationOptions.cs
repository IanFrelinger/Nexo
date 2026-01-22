namespace Nexo.GeoTerrain;

/// <summary>
/// Options for generating contour lines (isolines) from an <see cref="ElevationGrid"/>.
/// </summary>
public sealed record ContourGenerationOptions
{
    /// <summary>
    /// Contour interval in meters.
    /// </summary>
    public double IntervalMeters { get; init; } = 10.0;

    /// <summary>
    /// Optional minimum contour level (meters). If not set, derived from grid min.
    /// </summary>
    public double? MinElevationMeters { get; init; }

    /// <summary>
    /// Optional maximum contour level (meters). If not set, derived from grid max.
    /// </summary>
    public double? MaxElevationMeters { get; init; }

    /// <summary>
    /// If true, NaN samples are treated as 0m; otherwise generation throws on NaNs.
    /// </summary>
    public bool TreatNoDataAsZero { get; init; }

    /// <summary>
    /// Vertical scale applied to output Y coordinates (use same scale as mesh generation).
    /// </summary>
    public float VerticalScale { get; init; } = 1.0f;

    /// <summary>
    /// Quantization size (meters) used to stitch segments into polylines.
    /// Larger values are more tolerant to float jitter but may merge distinct endpoints.
    /// </summary>
    public double EndpointQuantizationMeters { get; init; } = 0.01;
}

