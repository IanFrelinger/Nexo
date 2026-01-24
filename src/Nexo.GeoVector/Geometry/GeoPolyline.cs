using Nexo.GeoTerrain;

namespace Nexo.GeoVector.Geometry;

/// <summary>
/// Geographic polyline defined by a sequence of points.
/// </summary>
public sealed class GeoPolyline : IGeoGeometry
{
    public IReadOnlyList<GeoPoint> Points { get; }

    public GeoPolyline(IReadOnlyList<GeoPoint> points)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(points);
#else
        if (points is null) throw new ArgumentNullException(nameof(points));
#endif
        if (points.Count < 2)
            throw new ArgumentException("Polyline must have at least 2 points.", nameof(points));
        Points = points;
    }
}

