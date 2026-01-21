namespace Nexo.GeoTerrain;

/// <summary>
/// Deterministic mesh generator: converts a regular elevation grid into a triangle mesh.
/// </summary>
public static class GridMeshGenerator
{
    public static MeshData Generate(ElevationGrid grid, MeshGenerationOptions? options = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(grid);
#else
        if (grid is null) throw new ArgumentNullException(nameof(grid));
#endif
        options ??= new MeshGenerationOptions();

        if (grid.Width < 2 || grid.Height < 2)
            throw new InvalidOperationException("ElevationGrid must be at least 2x2 to generate triangles.");

        var w = grid.Width;
        var h = grid.Height;

        // One vertex per sample.
        var vertices = new Vector3[checked(w * h)];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var elev = grid.GetHeightMeters(x, y);
                if (float.IsNaN(elev))
                {
                    if (!options.TreatNoDataAsZero)
                        throw new InvalidOperationException($"NoData/NaN elevation at ({x},{y}).");
                    elev = 0f;
                }

                var px = (float)(x * grid.Spacing.MetersX);
                var pz = (float)(y * grid.Spacing.MetersY);
                var py = elev * options.VerticalScale;
                vertices[ToIndex(x, y, w)] = new Vector3(px, py, pz);
            }
        }

        // Two triangles per cell: (w-1)*(h-1)*2 triangles, 3 indices each.
        var triCount = checked((w - 1) * (h - 1) * 2);
        var indices = new int[checked(triCount * 3)];
        var ii = 0;
        for (var y = 0; y < h - 1; y++)
        {
            for (var x = 0; x < w - 1; x++)
            {
                var v00 = ToIndex(x, y, w);
                var v10 = ToIndex(x + 1, y, w);
                var v01 = ToIndex(x, y + 1, w);
                var v11 = ToIndex(x + 1, y + 1, w);

                // Triangle 1: v00, v01, v10
                indices[ii++] = v00;
                indices[ii++] = v01;
                indices[ii++] = v10;

                // Triangle 2: v10, v01, v11
                indices[ii++] = v10;
                indices[ii++] = v01;
                indices[ii++] = v11;
            }
        }

        Vector3[]? normals = null;
        if (options.GenerateNormals)
        {
            normals = ComputeVertexNormals(vertices, indices);
        }

        return new MeshData
        {
            Vertices = vertices,
            Indices = indices,
            Normals = normals
        };
    }

    private static Vector3[] ComputeVertexNormals(Vector3[] vertices, int[] indices)
    {
        var normals = new Vector3[vertices.Length];

        // Accumulate face normals per vertex.
        for (var i = 0; i < indices.Length; i += 3)
        {
            var i0 = indices[i];
            var i1 = indices[i + 1];
            var i2 = indices[i + 2];

            var v0 = vertices[i0];
            var v1 = vertices[i1];
            var v2 = vertices[i2];

            var e1 = v1 - v0;
            var e2 = v2 - v0;
            var face = Vector3.Cross(e1, e2);

            normals[i0] = normals[i0] + face;
            normals[i1] = normals[i1] + face;
            normals[i2] = normals[i2] + face;
        }

        // Normalize.
        for (var i = 0; i < normals.Length; i++)
        {
            normals[i] = normals[i].Normalized();
        }

        return normals;
    }

    private static int ToIndex(int x, int y, int width) => checked(y * width + x);
}

