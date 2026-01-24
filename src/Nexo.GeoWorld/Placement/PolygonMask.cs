using Local2 = Nexo.GeoVector.Geometry.Vector2;

namespace Nexo.GeoWorld.Placement;

public sealed class PolygonMask : IPlacementMask
{
    public IReadOnlyList<Local2> RingMeters { get; }

    public PolygonMask(IReadOnlyList<Local2> ringMeters)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(ringMeters);
#else
        if (ringMeters is null) throw new ArgumentNullException(nameof(ringMeters));
#endif
        if (ringMeters.Count < 3)
            throw new ArgumentException("Polygon ring must have at least 3 points.", nameof(ringMeters));
        RingMeters = ringMeters;
    }

    public bool Contains(Local2 pointMeters)
        => Geometry2D.PointInPolygon(pointMeters, RingMeters);
}

