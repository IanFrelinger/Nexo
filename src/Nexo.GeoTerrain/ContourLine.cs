namespace Nexo.GeoTerrain;

/// <summary>
/// A single contour polyline at a constant elevation.
/// Points are in local meters (X/Z), with Y set to elevation*verticalScale.
/// </summary>
public sealed record ContourLine
{
    public required float ElevationMeters { get; init; }
    public required IReadOnlyList<Vector3> Points { get; init; }
}

