using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Projection;
using Nexo.GeoVector.Geometry;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;

namespace Nexo.GeoVector.Generation;

/// <summary>
/// Deterministic polyline-to-mesh generator for roads (ribbon mesh).
/// </summary>
public static class RoadMeshGenerator
{
    public static MeshData GenerateRoads(
        GeoFeatureSet roads,
        GeoPoint localOrigin,
        ElevationGrid? terrainGrid,
        RoadMeshGenerationOptions options,
        ICoordinateProjector? projector = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(roads);
        ArgumentNullException.ThrowIfNull(localOrigin);
        ArgumentNullException.ThrowIfNull(options);
#else
        if (roads is null) throw new ArgumentNullException(nameof(roads));
        if (localOrigin is null) throw new ArgumentNullException(nameof(localOrigin));
        if (options is null) throw new ArgumentNullException(nameof(options));
#endif
        if (options.GenerateTexCoords && options.UvMetersPerRepeat <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), options.UvMetersPerRepeat, "UvMetersPerRepeat must be > 0.");

        var v = new List<Vector3>(capacity: 2048);
        var n = new List<Vector3>(capacity: 2048);
        var uv = options.GenerateTexCoords ? new List<Nexo.GeoTerrain.Vector2>(capacity: 2048) : null;
        var tris = new List<int>(capacity: 4096);

        var proj = projector ?? EquirectangularProjector.Instance;
        var polylines = new List<RoadPolyline>(capacity: Math.Max(8, roads.Features.Count));

        foreach (var f in roads.Features)
        {
            if (!f.Kind.Equals(FeatureKind.Road)) continue;
            if (f.Geometry is not GeoPolyline line) continue;
            if (line.Points.Count < 2) continue;

            // Project to local meters (X east, Y north) -> map to X,Z.
            var local = GeoProjector.ProjectRingToLocalMeters(line.Points, localOrigin, proj);
            if (local.Length < 2) continue;

            polylines.Add(new RoadPolyline
            {
                GeoPoints = line.Points.ToArray(),
                Local = local
            });
        }

        if (!options.EnableIntersections || polylines.Count <= 1)
        {
            // Old behavior: independent ribbons.
            for (var pi = 0; pi < polylines.Count; pi++)
            {
                AppendRibbon(polylines[pi].Local, polylines[pi].GeoPoints, options.DefaultWidthMeters, terrainGrid, options, v, n, uv, tris);
            }
        }
        else
        {
            var radius = options.JunctionRadiusMeters > 0 ? options.JunctionRadiusMeters : options.DefaultWidthMeters * 0.6f;
            var intersections = FindIntersections(polylines);

            // Split polylines around junction discs.
            for (var pi = 0; pi < polylines.Count; pi++)
            {
                var pieces = SplitPolyline(polylines[pi].Local, intersections, pi, radius);
                for (var k = 0; k < pieces.Count; k++)
                {
                    AppendRibbon(pieces[k], polylines[pi].GeoPoints, options.DefaultWidthMeters, terrainGrid, options, v, n, uv, tris);
                }
            }

            // Add simple junction discs.
            for (var ii = 0; ii < intersections.Count; ii++)
            {
                AppendJunctionDisc(intersections[ii].Point, radius, terrainGrid, options, v, n, uv, tris);
            }
        }

        return new MeshData
        {
            Vertices = v,
            Indices = tris,
            Normals = n,
            TexCoords = uv
        };
    }

    private sealed class RoadPolyline
    {
        public required GeoPoint[] GeoPoints { get; init; }
        public required Nexo.GeoVector.Geometry.Vector2[] Local { get; init; }
    }

    private sealed class Intersection
    {
        public required Nexo.GeoVector.Geometry.Vector2 Point { get; init; }
        public required List<(int polyIdx, int segIdx, float t)> Incidents { get; init; }
    }

    private static List<Intersection> FindIntersections(List<RoadPolyline> polylines)
    {
        // Quantize to merge near-equal intersection points deterministically.
        const float eps = 1e-3f;
        var inv = 1f / eps;
        var dict = new Dictionary<(int qx, int qy), Intersection>();

        for (var i = 0; i < polylines.Count; i++)
        {
            var a = polylines[i].Local;
            for (var si = 0; si < a.Length - 1; si++)
            {
                var a0 = a[si];
                var a1 = a[si + 1];
                for (var j = i; j < polylines.Count; j++)
                {
                    var b = polylines[j].Local;
                    var sjStart = (j == i) ? si + 2 : 0; // skip adjacent segments on same polyline
                    for (var sj = sjStart; sj < b.Length - 1; sj++)
                    {
                        var b0 = b[sj];
                        var b1 = b[sj + 1];
                        if (!TrySegmentIntersection(a0, a1, b0, b1, out var p, out var ta, out var tb))
                            continue;

                        // Exclude endpoint-touching.
                        if (ta <= 1e-4f || ta >= 1f - 1e-4f) continue;
                        if (tb <= 1e-4f || tb >= 1f - 1e-4f) continue;

                        var qx = (int)Math.Round(p.X * inv);
                        var qy = (int)Math.Round(p.Y * inv);
                        var key = (qx, qy);
                        if (!dict.TryGetValue(key, out var inter))
                        {
                            inter = new Intersection { Point = p, Incidents = new List<(int, int, float)>() };
                            dict[key] = inter;
                        }
                        inter.Incidents.Add((i, si, ta));
                        inter.Incidents.Add((j, sj, tb));
                    }
                }
            }
        }

        var list = dict.Values.ToList();
        // Stable order by coordinates.
        list.Sort((x, y) =>
        {
            var c = x.Point.X.CompareTo(y.Point.X);
            if (c != 0) return c;
            return x.Point.Y.CompareTo(y.Point.Y);
        });
        return list;
    }

    private static List<Nexo.GeoVector.Geometry.Vector2[]> SplitPolyline(Nexo.GeoVector.Geometry.Vector2[] pts, List<Intersection> intersections, int polyIdx, float radius)
    {
        // Collect cuts per segment.
        var bySeg = new Dictionary<int, List<float>>();
        for (var i = 0; i < intersections.Count; i++)
        {
            var inc = intersections[i].Incidents;
            for (var k = 0; k < inc.Count; k++)
            {
                if (inc[k].polyIdx != polyIdx) continue;
                if (!bySeg.TryGetValue(inc[k].segIdx, out var list))
                {
                    list = new List<float>();
                    bySeg[inc[k].segIdx] = list;
                }
                list.Add(inc[k].t);
            }
        }

        var outPolys = new List<Nexo.GeoVector.Geometry.Vector2[]>();
        var current = new List<Nexo.GeoVector.Geometry.Vector2>();
        current.Add(pts[0]);

        for (var si = 0; si < pts.Length - 1; si++)
        {
            var a = pts[si];
            var b = pts[si + 1];
            var seg = new Nexo.GeoVector.Geometry.Vector2(b.X - a.X, b.Y - a.Y);
            var segLen = (float)Math.Sqrt(seg.X * seg.X + seg.Y * seg.Y + 1e-12f);
            var dir = new Nexo.GeoVector.Geometry.Vector2(seg.X / segLen, seg.Y / segLen);

            if (!bySeg.TryGetValue(si, out var cuts))
            {
                current.Add(b);
                continue;
            }

            cuts.Sort();
            var lastT = 0f;
            for (var ci = 0; ci < cuts.Count; ci++)
            {
                var t = cuts[ci];
                if (t <= lastT) continue;
                lastT = t;

                var dist = t * segLen;
                var beforeDist = Math.Max(0f, dist - radius);
                var afterDist = Math.Min(segLen, dist + radius);
                var pBefore = new Nexo.GeoVector.Geometry.Vector2(a.X + dir.X * beforeDist, a.Y + dir.Y * beforeDist);
                var pAfter = new Nexo.GeoVector.Geometry.Vector2(a.X + dir.X * afterDist, a.Y + dir.Y * afterDist);

                if (current.Count == 0 || (current[current.Count - 1].X != pBefore.X || current[current.Count - 1].Y != pBefore.Y))
                    current.Add(pBefore);

                if (current.Count >= 2)
                {
                    outPolys.Add(current.ToArray());
                }

                current = new List<Nexo.GeoVector.Geometry.Vector2>();
                current.Add(pAfter);
            }

            current.Add(b);
        }

        if (current.Count >= 2)
            outPolys.Add(current.ToArray());

        return outPolys;
    }

    private static void AppendRibbon(
        Nexo.GeoVector.Geometry.Vector2[] local,
        GeoPoint[] geoPoints,
        float width,
        ElevationGrid? terrainGrid,
        RoadMeshGenerationOptions options,
        List<Vector3> v,
        List<Vector3> n,
        List<Nexo.GeoTerrain.Vector2>? uv,
        List<int> tris)
    {
        if (local.Length < 2) return;

        var half = width * 0.5f;
        var startIndex = v.Count;
        var accum = 0f;

        for (var i = 0; i < local.Length; i++)
        {
            var p = local[i];
            var dir = ComputeDirection(local, i);
            var perp = new Nexo.GeoVector.Geometry.Vector2(-dir.Y, dir.X); // left
            var invLen = 1.0f / (float)Math.Sqrt(perp.X * perp.X + perp.Y * perp.Y + 1e-12f);
            perp = new Nexo.GeoVector.Geometry.Vector2(perp.X * invLen, perp.Y * invLen);

            var left2 = new Nexo.GeoVector.Geometry.Vector2(p.X + perp.X * half, p.Y + perp.Y * half);
            var right2 = new Nexo.GeoVector.Geometry.Vector2(p.X - perp.X * half, p.Y - perp.Y * half);

            // Use nearest available geo point for stable terrain sampling.
            var geo = geoPoints.Length > 0 ? geoPoints[Math.Min(i, geoPoints.Length - 1)] : new GeoPoint { Latitude = new Latitude(0), Longitude = new Longitude(0) };
            var yLeft = ResolveHeight(terrainGrid, geo, left2.X, left2.Y, options);
            var yRight = ResolveHeight(terrainGrid, geo, right2.X, right2.Y, options);

            v.Add(new Vector3(left2.X, yLeft, left2.Y));
            v.Add(new Vector3(right2.X, yRight, right2.Y));
            n.Add(new Vector3(0, 1, 0));
            n.Add(new Vector3(0, 1, 0));

            if (uv != null)
            {
                if (i > 0)
                {
                    var prev = local[i - 1];
                    var dx = p.X - prev.X;
                    var dy = p.Y - prev.Y;
                    accum += (float)Math.Sqrt(dx * dx + dy * dy);
                }

                var uCoord = accum / options.UvMetersPerRepeat;
                uv.Add(new Nexo.GeoTerrain.Vector2(uCoord, 0));
                uv.Add(new Nexo.GeoTerrain.Vector2(uCoord, width / options.UvMetersPerRepeat));
            }
        }

        for (var i = 0; i < local.Length - 1; i++)
        {
            var i0L = startIndex + i * 2;
            var i0R = startIndex + i * 2 + 1;
            var i1L = startIndex + (i + 1) * 2;
            var i1R = startIndex + (i + 1) * 2 + 1;

            tris.Add(i0L);
            tris.Add(i1R);
            tris.Add(i1L);

            tris.Add(i0L);
            tris.Add(i0R);
            tris.Add(i1R);
        }
    }

    private static void AppendJunctionDisc(
        Nexo.GeoVector.Geometry.Vector2 p,
        float radius,
        ElevationGrid? terrainGrid,
        RoadMeshGenerationOptions options,
        List<Vector3> v,
        List<Vector3> n,
        List<Nexo.GeoTerrain.Vector2>? uv,
        List<int> tris)
    {
        const int segments = 16;
        var centerIndex = v.Count;
        var y = 0f;
        if (terrainGrid != null && options.ConformToTerrain)
        {
            if (ElevationGridSampler.TrySampleHeightMetersAtLocalXZ(terrainGrid, p.X, p.Y, options.TreatNoDataAsZero, out var h))
                y = h;
        }

        v.Add(new Vector3(p.X, y, p.Y));
        n.Add(new Vector3(0, 1, 0));
        if (uv != null) uv.Add(new Nexo.GeoTerrain.Vector2(0, 0));

        for (var i = 0; i <= segments; i++)
        {
            var ang = (i / (float)segments) * (float)(Math.PI * 2.0);
            var x = p.X + (float)Math.Cos(ang) * radius;
            var z = p.Y + (float)Math.Sin(ang) * radius;
            v.Add(new Vector3(x, y, z));
            n.Add(new Vector3(0, 1, 0));
            if (uv != null) uv.Add(new Nexo.GeoTerrain.Vector2(x / options.UvMetersPerRepeat, z / options.UvMetersPerRepeat));
        }

        for (var i = 0; i < segments; i++)
        {
            tris.Add(centerIndex);
            tris.Add(centerIndex + i + 1);
            tris.Add(centerIndex + i + 2);
        }
    }

    private static bool TrySegmentIntersection(
        Nexo.GeoVector.Geometry.Vector2 a0,
        Nexo.GeoVector.Geometry.Vector2 a1,
        Nexo.GeoVector.Geometry.Vector2 b0,
        Nexo.GeoVector.Geometry.Vector2 b1,
        out Nexo.GeoVector.Geometry.Vector2 p,
        out float ta,
        out float tb)
    {
        // Solve a0 + ta*(a1-a0) == b0 + tb*(b1-b0)
        var r = new Nexo.GeoVector.Geometry.Vector2(a1.X - a0.X, a1.Y - a0.Y);
        var s = new Nexo.GeoVector.Geometry.Vector2(b1.X - b0.X, b1.Y - b0.Y);
        var denom = r.X * s.Y - r.Y * s.X;
        if (Math.Abs(denom) < 1e-8f)
        {
            p = default;
            ta = tb = 0;
            return false;
        }

        var qp = new Nexo.GeoVector.Geometry.Vector2(b0.X - a0.X, b0.Y - a0.Y);
        ta = (qp.X * s.Y - qp.Y * s.X) / denom;
        tb = (qp.X * r.Y - qp.Y * r.X) / denom;
        if (ta < 0 || ta > 1 || tb < 0 || tb > 1)
        {
            p = default;
            return false;
        }

        p = new Nexo.GeoVector.Geometry.Vector2(a0.X + r.X * ta, a0.Y + r.Y * ta);
        return true;
    }

    private static Nexo.GeoVector.Geometry.Vector2 ComputeDirection(Nexo.GeoVector.Geometry.Vector2[] poly, int i)
    {
        if (poly.Length < 2) return new Nexo.GeoVector.Geometry.Vector2(1, 0);
        if (i == 0) return Normalize(new Nexo.GeoVector.Geometry.Vector2(poly[1].X - poly[0].X, poly[1].Y - poly[0].Y));
        if (i == poly.Length - 1) return Normalize(new Nexo.GeoVector.Geometry.Vector2(poly[i].X - poly[i - 1].X, poly[i].Y - poly[i - 1].Y));

        var a = Normalize(new Nexo.GeoVector.Geometry.Vector2(poly[i].X - poly[i - 1].X, poly[i].Y - poly[i - 1].Y));
        var b = Normalize(new Nexo.GeoVector.Geometry.Vector2(poly[i + 1].X - poly[i].X, poly[i + 1].Y - poly[i].Y));
        return Normalize(new Nexo.GeoVector.Geometry.Vector2(a.X + b.X, a.Y + b.Y));
    }

    private static Nexo.GeoVector.Geometry.Vector2 Normalize(Nexo.GeoVector.Geometry.Vector2 v)
    {
        var len = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + 1e-12f);
        return new Nexo.GeoVector.Geometry.Vector2(v.X / len, v.Y / len);
    }

    private static float ResolveHeight(ElevationGrid? grid, GeoPoint geo, float xMeters, float zMeters, RoadMeshGenerationOptions options)
    {
        if (options.ConstantHeightMeters.HasValue) return options.ConstantHeightMeters.Value;
        if (grid == null || !options.ConformToTerrain) return 0f;

        // Prefer geo sampling (stable across local projectors).
        if (ElevationGridSampler.TrySampleHeightMetersAtGeoPoint(grid, geo, options.TreatNoDataAsZero, out var hGeo))
        {
            return hGeo;
        }

        // Fallback: local sampling.
        if (ElevationGridSampler.TrySampleHeightMetersAtLocalXZ(grid, xMeters, zMeters, options.TreatNoDataAsZero, out var hLocal))
        {
            return hLocal;
        }

        return 0f;
    }
}

