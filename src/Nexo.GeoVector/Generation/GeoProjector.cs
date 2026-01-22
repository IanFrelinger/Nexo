using Nexo.GeoTerrain;
using Local2 = Nexo.GeoVector.Geometry.Vector2;

namespace Nexo.GeoVector.Generation;

/// <summary>
/// Deterministic projector from lat/lon degrees to a local tangent-plane approximation (meters).
/// Good enough for small-ish bounds (tiles/regions), portable and dependency-free.
/// </summary>
public static class GeoProjector
{
    public static Local2[] ProjectRingToLocalMeters(IReadOnlyList<GeoPoint> ring, GeoPoint origin)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(ring);
        ArgumentNullException.ThrowIfNull(origin);
#else
        if (ring is null) throw new ArgumentNullException(nameof(ring));
        if (origin is null) throw new ArgumentNullException(nameof(origin));
#endif
        if (ring.Count == 0) return Array.Empty<Local2>();

        // Equirectangular approximation around origin latitude.
        // meters per degree latitude ~ 111,320
        // meters per degree longitude ~ 111,320 * cos(lat0)
        var lat0Rad = origin.Latitude.Degrees * (Math.PI / 180.0);
        var mPerDegLat = 111_320.0;
        var mPerDegLon = 111_320.0 * Math.Cos(lat0Rad);

        var ox = origin.Longitude.Degrees;
        var oy = origin.Latitude.Degrees;

        var result = new Local2[ring.Count];
        for (var i = 0; i < ring.Count; i++)
        {
            var p = ring[i];
            var dx = (p.Longitude.Degrees - ox) * mPerDegLon;
            var dy = (p.Latitude.Degrees - oy) * mPerDegLat;
            result[i] = new Local2((float)dx, (float)dy);
        }
        return result;
    }
}

