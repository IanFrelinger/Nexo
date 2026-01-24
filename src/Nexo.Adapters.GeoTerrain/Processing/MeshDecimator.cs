using System.Globalization;
using Nexo.GeoTerrain;

namespace Nexo.Adapters.GeoTerrain.Processing;

/// <summary>
/// Deterministic mesh simplifier using vertex clustering (grid quantization).
/// This is not as high-quality as QEM, but it is portable, deterministic, and fast.
/// </summary>
public static class MeshDecimator
{
    public static MeshData SimplifyToTargetTriangles(MeshData mesh, int targetTriangles, float minCellSizeMeters = 0.1f, float maxCellSizeMeters = 200.0f, int maxIterations = 12)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(mesh);
#else
        if (mesh is null) throw new ArgumentNullException(nameof(mesh));
#endif
        if (targetTriangles <= 0) throw new ArgumentOutOfRangeException(nameof(targetTriangles), targetTriangles, "targetTriangles must be > 0.");
        if (mesh.Indices.Count / 3 <= targetTriangles) return mesh;

        var lo = Math.Max(1e-5f, minCellSizeMeters);
        var hi = Math.Max(lo, maxCellSizeMeters);

        // Ensure hi is large enough to reduce below target (or we bail out at max).
        MeshData simplifiedHi = mesh;
        for (var expand = 0; expand < 12; expand++)
        {
            simplifiedHi = SimplifyByVertexClustering(mesh, hi);
            if (simplifiedHi.Indices.Count / 3 <= targetTriangles) break;
            hi *= 2f;
        }

        MeshData? best = null;
        for (var i = 0; i < maxIterations; i++)
        {
            var mid = (lo + hi) * 0.5f;
            var s = SimplifyByVertexClustering(mesh, mid);
            var tris = s.Indices.Count / 3;
            if (tris <= targetTriangles)
            {
                best = s;
                hi = mid;
            }
            else
            {
                lo = mid;
            }
        }

        return best ?? simplifiedHi;
    }

    public static MeshData SimplifyByVertexClustering(MeshData mesh, float cellSizeMeters)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(mesh);
#else
        if (mesh is null) throw new ArgumentNullException(nameof(mesh));
#endif
        if (cellSizeMeters <= 0) throw new ArgumentOutOfRangeException(nameof(cellSizeMeters), cellSizeMeters, "cellSizeMeters must be > 0.");
        if (mesh.Vertices.Count == 0 || mesh.Indices.Count == 0) return mesh;

        var hasNormals = mesh.Normals != null && mesh.Normals.Count == mesh.Vertices.Count;
        var hasUv = mesh.TexCoords != null && mesh.TexCoords.Count == mesh.Vertices.Count;

        var inv = 1f / cellSizeMeters;

        var clusterByKey = new Dictionary<ClusterKey, ClusterAccum>(capacity: Math.Min(mesh.Vertices.Count, 1 << 20));
        var oldToNew = new int[mesh.Vertices.Count];

        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            var key = new ClusterKey(
                qx: Quantize(v.X, inv),
                qy: Quantize(v.Y, inv),
                qz: Quantize(v.Z, inv));

            if (!clusterByKey.TryGetValue(key, out var acc))
            {
                acc = new ClusterAccum
                {
                    Index = clusterByKey.Count,
                    SumX = 0,
                    SumY = 0,
                    SumZ = 0,
                    SumNx = 0,
                    SumNy = 0,
                    SumNz = 0,
                    SumU = 0,
                    SumV = 0,
                    Count = 0
                };
            }

            acc.SumX += v.X;
            acc.SumY += v.Y;
            acc.SumZ += v.Z;
            if (hasNormals)
            {
                var n = mesh.Normals![i];
                acc.SumNx += n.X;
                acc.SumNy += n.Y;
                acc.SumNz += n.Z;
            }
            if (hasUv)
            {
                var t = mesh.TexCoords![i];
                acc.SumU += t.X;
                acc.SumV += t.Y;
            }
            acc.Count++;

            clusterByKey[key] = acc;
            oldToNew[i] = acc.Index;
        }

        var newVerts = new Vector3[clusterByKey.Count];
        Vector3[]? newNormals = hasNormals ? new Vector3[clusterByKey.Count] : null;
        Vector2[]? newUvs = hasUv ? new Vector2[clusterByKey.Count] : null;

        foreach (var kvp in clusterByKey)
        {
            var acc = kvp.Value;
            var invCount = 1f / Math.Max(1, acc.Count);
            newVerts[acc.Index] = new Vector3(acc.SumX * invCount, acc.SumY * invCount, acc.SumZ * invCount);
            if (newNormals != null)
            {
                var nx = acc.SumNx * invCount;
                var ny = acc.SumNy * invCount;
                var nz = acc.SumNz * invCount;
                var len = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz + 1e-12f);
                newNormals[acc.Index] = new Vector3(nx / len, ny / len, nz / len);
            }
            if (newUvs != null)
            {
                newUvs[acc.Index] = new Vector2(acc.SumU * invCount, acc.SumV * invCount);
            }
        }

        // Rebuild indices; drop degenerate triangles.
        var newIndices = new List<int>(capacity: mesh.Indices.Count);
        for (var i = 0; i < mesh.Indices.Count; i += 3)
        {
            var a = oldToNew[mesh.Indices[i]];
            var b = oldToNew[mesh.Indices[i + 1]];
            var c = oldToNew[mesh.Indices[i + 2]];
            if (a == b || b == c || a == c) continue;
            newIndices.Add(a);
            newIndices.Add(b);
            newIndices.Add(c);
        }

        // If normals were absent, recompute a basic set (helps glTF export).
        if (!hasNormals)
        {
            newNormals = ComputeVertexNormals(newVerts, newIndices);
        }

        return new MeshData
        {
            Vertices = newVerts,
            Indices = newIndices,
            Normals = newNormals,
            TexCoords = newUvs
        };
    }

    private static int Quantize(float v, float invCell)
        => (int)MathF.Floor(v * invCell);

    private static Vector3[] ComputeVertexNormals(IReadOnlyList<Vector3> vertices, IReadOnlyList<int> indices)
    {
        var normals = new Vector3[vertices.Count];
        for (var i = 0; i < indices.Count; i += 3)
        {
            var ia = indices[i];
            var ib = indices[i + 1];
            var ic = indices[i + 2];
            var a = vertices[ia];
            var b = vertices[ib];
            var c = vertices[ic];
            var ab = new Vector3(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
            var ac = new Vector3(c.X - a.X, c.Y - a.Y, c.Z - a.Z);
            var nx = ab.Y * ac.Z - ab.Z * ac.Y;
            var ny = ab.Z * ac.X - ab.X * ac.Z;
            var nz = ab.X * ac.Y - ab.Y * ac.X;
            normals[ia] = new Vector3(normals[ia].X + nx, normals[ia].Y + ny, normals[ia].Z + nz);
            normals[ib] = new Vector3(normals[ib].X + nx, normals[ib].Y + ny, normals[ib].Z + nz);
            normals[ic] = new Vector3(normals[ic].X + nx, normals[ic].Y + ny, normals[ic].Z + nz);
        }

        for (var i = 0; i < normals.Length; i++)
        {
            var n = normals[i];
            var len = (float)Math.Sqrt(n.X * n.X + n.Y * n.Y + n.Z * n.Z + 1e-12f);
            normals[i] = new Vector3(n.X / len, n.Y / len, n.Z / len);
        }
        return normals;
    }

    private readonly record struct ClusterKey(int qx, int qy, int qz);

    private sealed class ClusterAccum
    {
        public int Index;
        public float SumX;
        public float SumY;
        public float SumZ;
        public float SumNx;
        public float SumNy;
        public float SumNz;
        public float SumU;
        public float SumV;
        public int Count;
    }
}

