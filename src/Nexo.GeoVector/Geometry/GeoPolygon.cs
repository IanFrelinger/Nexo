using Nexo.GeoTerrain;

namespace Nexo.GeoVector.Geometry;

/// <summary>
/// Geographic polygon defined by an outer ring of geo points (no holes for v0).
/// Ring is expected to be closed or will be treated as implicitly closed.
/// </summary>
public sealed class GeoPolygon
{
    public IReadOnlyList<GeoPoint> OuterRing { get; }

    public GeoPolygon(IReadOnlyList<GeoPoint> outerRing)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(outerRing);
#else
        if (outerRing is null) throw new ArgumentNullException(nameof(outerRing));
#endif
        if (outerRing.Count < 3)
            throw new ArgumentException("Polygon ring must have at least 3 points.", nameof(outerRing));
        OuterRing = outerRing;
    }
}

