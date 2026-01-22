using Nexo.GeoTerrain;
using Local2 = Nexo.GeoVector.Geometry.Vector2;
using Uv2 = Nexo.GeoTerrain.Vector2;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;

namespace Nexo.GeoVector.Generation;

/// <summary>
/// Deterministic building footprint extrusion to triangle mesh.
/// v0 supports simple polygons without holes.
/// </summary>
public static class BuildingMeshGenerator
{
    public static MeshData GenerateBuildings(
        GeoFeatureSet features,
        GeoPoint localOrigin,
        BuildingExtrusionOptions? options = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(features);
#else
        if (features is null) throw new ArgumentNullException(nameof(features));
#endif
        options ??= new BuildingExtrusionOptions();

        var vertices = new List<Vector3>(capacity: 4096);
        var indices = new List<int>(capacity: 8192);
        List<Uv2>? texCoords = null;
        if (options.GenerateTexCoords)
        {
            if (options.UvMetersPerRepeat <= 0)
                throw new ArgumentOutOfRangeException(nameof(options), options.UvMetersPerRepeat, "UvMetersPerRepeat must be > 0.");
            texCoords = new List<Uv2>(capacity: 4096);
        }

        foreach (var f in features.Features)
        {
            if (!f.Kind.Equals(FeatureKind.Building)) continue;

            var ring = f.Geometry.OuterRing;
            var local2 = GeoProjector.ProjectRingToLocalMeters(ring, localOrigin);

            // Normalize ring: drop duplicate closing point if provided.
            if (local2.Length >= 4 && NearlyEqual(local2[0], local2[local2.Length - 1]))
            {
                Array.Resize(ref local2, local2.Length - 1);
            }

            if (local2.Length < 3) continue;

            var height = ResolveHeightMeters(f, options);
            var n = local2.Length;

            // Ensure CCW winding for consistent perimeter UVs + wall winding.
            if (SignedArea(local2) < 0)
            {
                Array.Reverse(local2);
            }

            // Perimeter cumulative distance (meters) for walls UVs.
            var cum = new float[n + 1];
            for (var i = 0; i < n; i++)
            {
                var a = local2[i];
                var b = local2[(i + 1) % n];
                cum[i + 1] = cum[i] + Distance(a, b);
            }

            var metersPerRepeat = options.UvMetersPerRepeat;
            var invRepeat = metersPerRepeat > 0 ? (1.0f / metersPerRepeat) : 1.0f;

            var tri = EarClipTriangulator.Triangulate(local2);
            if (tri.Count == 0) continue;

            // Roof vertices (duplicated to allow roof UVs independent of walls).
            var roofBaseIndex = vertices.Count;
            for (var i = 0; i < n; i++)
            {
                var p = local2[i];
                vertices.Add(new Vector3(p.X, height, p.Y));
                if (texCoords != null)
                {
                    // Planar mapping in meters: x/z
                    texCoords.Add(new Uv2(p.X * invRepeat, p.Y * invRepeat));
                }
            }

            // Top faces (roughly +Y)
            for (var i = 0; i < tri.Count; i += 3)
            {
                var a = roofBaseIndex + tri[i];
                var b = roofBaseIndex + tri[i + 1];
                var c = roofBaseIndex + tri[i + 2];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);
            }

            // Optional bottom faces (reverse winding)
            if (options.IncludeBottom)
            {
                var bottomBaseIndex = vertices.Count;
                for (var i = 0; i < n; i++)
                {
                    var p = local2[i];
                    vertices.Add(new Vector3(p.X, 0, p.Y));
                    if (texCoords != null)
                    {
                        texCoords.Add(new Uv2(p.X * invRepeat, p.Y * invRepeat));
                    }
                }

                for (var i = 0; i < tri.Count; i += 3)
                {
                    var a = bottomBaseIndex + tri[i];
                    var b = bottomBaseIndex + tri[i + 1];
                    var c = bottomBaseIndex + tri[i + 2];
                    indices.Add(c);
                    indices.Add(b);
                    indices.Add(a);
                }
            }

            // Walls (duplicate vertices per edge so UVs can be correct per surface).
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                var p0 = local2[i];
                var p1 = local2[j];

                var u0 = cum[i] * invRepeat;
                var u1 = cum[i + 1] * invRepeat;
                var v0 = 0f;
                var v1 = height * invRepeat;

                var wallBase = vertices.Count;

                // Order: b0, b1, t1, t0
                vertices.Add(new Vector3(p0.X, 0, p0.Y));
                vertices.Add(new Vector3(p1.X, 0, p1.Y));
                vertices.Add(new Vector3(p1.X, height, p1.Y));
                vertices.Add(new Vector3(p0.X, height, p0.Y));

                if (texCoords != null)
                {
                    texCoords.Add(new Uv2(u0, v0));
                    texCoords.Add(new Uv2(u1, v0));
                    texCoords.Add(new Uv2(u1, v1));
                    texCoords.Add(new Uv2(u0, v1));
                }

                // Two triangles (b0, t0, t1) and (b0, t1, b1)
                indices.Add(wallBase + 0);
                indices.Add(wallBase + 3);
                indices.Add(wallBase + 2);

                indices.Add(wallBase + 0);
                indices.Add(wallBase + 2);
                indices.Add(wallBase + 1);
            }
        }

        var normals = ComputeVertexNormals(vertices, indices);
        return new MeshData
        {
            Vertices = vertices,
            Indices = indices,
            Normals = normals,
            TexCoords = texCoords
        };
    }

    private static float ResolveHeightMeters(GeoFeature f, BuildingExtrusionOptions options)
    {
        if (f.Properties.TryGetValue("height_m", out var hm) && TryToSingle(hm, out var h1))
            return Math.Max(options.MinHeightMeters, h1);
        if (f.Properties.TryGetValue("height", out var h) && TryToSingle(h, out var h2))
            return Math.Max(options.MinHeightMeters, h2);
        if (f.Properties.TryGetValue("levels", out var lv) && TryToSingle(lv, out var levels))
            return Math.Max(options.MinHeightMeters, levels * 3.0f);
        return Math.Max(options.MinHeightMeters, options.DefaultHeightMeters);
    }

    private static bool TryToSingle(object value, out float result)
    {
        switch (value)
        {
            case float f:
                result = f; return true;
            case double d:
                result = (float)d; return true;
            case int i:
                result = i; return true;
            case long l:
                result = l; return true;
            case string s when float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                result = parsed; return true;
            default:
                result = 0;
                return false;
        }
    }

    private static bool NearlyEqual(Local2 a, Local2 b)
    {
        const float eps = 1e-5f;
        return Math.Abs(a.X - b.X) < eps && Math.Abs(a.Y - b.Y) < eps;
    }

    private static float SignedArea(Local2[] p)
    {
        double area = 0;
        for (var i = 0; i < p.Length; i++)
        {
            var a = p[i];
            var b = p[(i + 1) % p.Length];
            area += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        return (float)(area * 0.5);
    }

    private static float Distance(Local2 a, Local2 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    private static Vector3[] ComputeVertexNormals(IReadOnlyList<Vector3> vertices, IReadOnlyList<int> indices)
    {
        var n = new Vector3[vertices.Count];

        for (var i = 0; i < indices.Count; i += 3)
        {
            var ia = indices[i];
            var ib = indices[i + 1];
            var ic = indices[i + 2];

            var a = vertices[ia];
            var b = vertices[ib];
            var c = vertices[ic];

            var ab = Vector3.Subtract(b, a);
            var ac = Vector3.Subtract(c, a);
            var face = Cross(ab, ac);

            n[ia] = Vector3.Add(n[ia], face);
            n[ib] = Vector3.Add(n[ib], face);
            n[ic] = Vector3.Add(n[ic], face);
        }

        for (var i = 0; i < n.Length; i++)
        {
            n[i] = Normalize(n[i]);
        }

        return n;
    }

    private static Vector3 Cross(Vector3 a, Vector3 b)
        => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

    private static Vector3 Normalize(Vector3 v)
    {
        var len = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        if (len <= 1e-12f) return new Vector3(0, 1, 0);
        var inv = 1.0f / len;
        return new Vector3(v.X * inv, v.Y * inv, v.Z * inv);
    }
}

internal static class EarClipTriangulator
{
    public static List<int> Triangulate(IReadOnlyList<Local2> poly)
    {
        if (poly.Count < 3) return new List<int>();

        // Make a working index list.
        var idx = new List<int>(poly.Count);
        for (var i = 0; i < poly.Count; i++) idx.Add(i);

        // Ensure CCW winding.
        if (SignedArea(poly) < 0)
        {
            idx.Reverse();
        }

        var result = new List<int>(capacity: (poly.Count - 2) * 3);
        var guard = 0;

        while (idx.Count > 3 && guard++ < 10_000)
        {
            var earFound = false;
            for (var i = 0; i < idx.Count; i++)
            {
                var i0 = idx[(i - 1 + idx.Count) % idx.Count];
                var i1 = idx[i];
                var i2 = idx[(i + 1) % idx.Count];

                if (!IsConvex(poly[i0], poly[i1], poly[i2])) continue;
                if (ContainsAnyPoint(poly, idx, i0, i1, i2)) continue;

                result.Add(i0);
                result.Add(i1);
                result.Add(i2);
                idx.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
            {
                // Degenerate/self-intersecting polygon; give up deterministically.
                return new List<int>();
            }
        }

        if (idx.Count == 3)
        {
            result.Add(idx[0]);
            result.Add(idx[1]);
            result.Add(idx[2]);
        }

        return result;
    }

    private static float SignedArea(IReadOnlyList<Local2> p)
    {
        double area = 0;
        for (var i = 0; i < p.Count; i++)
        {
            var a = p[i];
            var b = p[(i + 1) % p.Count];
            area += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        return (float)(area * 0.5);
    }

    private static bool IsConvex(Local2 a, Local2 b, Local2 c)
    {
        // z-component of cross((b-a),(c-b)) in 2D
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var bcx = c.X - b.X;
        var bcy = c.Y - b.Y;
        var cross = abx * bcy - aby * bcx;
        return cross > 1e-6f;
    }

    private static bool ContainsAnyPoint(IReadOnlyList<Local2> poly, List<int> idx, int a, int b, int c)
    {
        var A = poly[a];
        var B = poly[b];
        var C = poly[c];

        for (var k = 0; k < idx.Count; k++)
        {
            var pIdx = idx[k];
            if (pIdx == a || pIdx == b || pIdx == c) continue;
            if (PointInTriangle(poly[pIdx], A, B, C)) return true;
        }
        return false;
    }

    private static bool PointInTriangle(Local2 p, Local2 a, Local2 b, Local2 c)
    {
        // Barycentric technique, including edge as inside.
        var v0 = new Local2(c.X - a.X, c.Y - a.Y);
        var v1 = new Local2(b.X - a.X, b.Y - a.Y);
        var v2 = new Local2(p.X - a.X, p.Y - a.Y);

        var dot00 = v0.X * v0.X + v0.Y * v0.Y;
        var dot01 = v0.X * v1.X + v0.Y * v1.Y;
        var dot02 = v0.X * v2.X + v0.Y * v2.Y;
        var dot11 = v1.X * v1.X + v1.Y * v1.Y;
        var dot12 = v1.X * v2.X + v1.Y * v2.Y;

        var denom = (dot00 * dot11 - dot01 * dot01);
        if (Math.Abs(denom) < 1e-12f) return false;
        var inv = 1.0f / denom;
        var u = (dot11 * dot02 - dot01 * dot12) * inv;
        var v = (dot00 * dot12 - dot01 * dot02) * inv;

        return u >= -1e-6f && v >= -1e-6f && (u + v) <= 1.0f + 1e-6f;
    }
}

