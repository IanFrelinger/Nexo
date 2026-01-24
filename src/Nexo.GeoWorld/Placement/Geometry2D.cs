using Local2 = Nexo.GeoVector.Geometry.Vector2;

namespace Nexo.GeoWorld.Placement;

internal static class Geometry2D
{
    // Ray casting point-in-polygon; ring may be closed or open.
    public static bool PointInPolygon(Local2 p, IReadOnlyList<Local2> ring)
    {
        if (ring.Count < 3) return false;

        var inside = false;
        var j = ring.Count - 1;
        for (var i = 0; i < ring.Count; i++)
        {
            var pi = ring[i];
            var pj = ring[j];

            var intersect = ((pi.Y > p.Y) != (pj.Y > p.Y)) &&
                            (p.X < (pj.X - pi.X) * (p.Y - pi.Y) / ((pj.Y - pi.Y) == 0 ? 1e-6f : (pj.Y - pi.Y)) + pi.X);
            if (intersect) inside = !inside;
            j = i;
        }

        return inside;
    }

    public static float DistancePointToSegment(Local2 p, Local2 a, Local2 b)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var apx = p.X - a.X;
        var apy = p.Y - a.Y;

        var abLen2 = abx * abx + aby * aby;
        if (abLen2 <= 1e-6f) return Distance(p, a);

        var t = (apx * abx + apy * aby) / abLen2;
        if (t < 0) t = 0;
        if (t > 1) t = 1;

        var cx = a.X + abx * t;
        var cy = a.Y + aby * t;
        return Distance(p, new Local2(cx, cy));
    }

    public static float Distance(Local2 a, Local2 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }
}

