using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Projection;
using Local2 = Nexo.GeoVector.Geometry.Vector2;

namespace Nexo.GeoVector.Generation;

/// <summary>
/// Deterministic projector from lat/lon degrees to a local tangent-plane approximation (meters).
/// Good enough for small-ish bounds (tiles/regions), portable and dependency-free.
/// </summary>
public static class GeoProjector
{
    public static Local2[] ProjectRingToLocalMeters(IReadOnlyList<GeoPoint> ring, GeoPoint origin)
        => ProjectRingToLocalMeters(ring, origin, EquirectangularProjector.Instance);

    public static Local2[] ProjectRingToLocalMeters(IReadOnlyList<GeoPoint> ring, GeoPoint origin, ICoordinateProjector projector)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(ring);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(projector);
#else
        if (ring is null) throw new ArgumentNullException(nameof(ring));
        if (origin is null) throw new ArgumentNullException(nameof(origin));
        if (projector is null) throw new ArgumentNullException(nameof(projector));
#endif
        if (ring.Count == 0) return Array.Empty<Local2>();

        var result = new Local2[ring.Count];
        for (var i = 0; i < ring.Count; i++)
        {
            var p = ring[i];
            var xy = projector.ProjectMeters(p, origin);
            result[i] = new Local2(xy.X, xy.Y);
        }
        return result;
    }
}

