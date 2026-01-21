namespace Nexo.GeoTerrain;

/// <summary>
/// Computes basic quality metrics for an elevation grid and/or a generated mesh.
/// </summary>
public static class MeshQualityAnalyzer
{
    public static MeshQualityReport Analyze(ElevationGrid grid, MeshData mesh)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(mesh);
#else
        if (grid is null) throw new ArgumentNullException(nameof(grid));
        if (mesh is null) throw new ArgumentNullException(nameof(mesh));
#endif
        if (mesh.Vertices is null) throw new ArgumentException("MeshData.Vertices must be non-null.", nameof(mesh));
        if (mesh.Indices is null) throw new ArgumentException("MeshData.Indices must be non-null.", nameof(mesh));

        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;
        var nodata = 0;

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var v = grid.GetHeightMeters(x, y);
                if (float.IsNaN(v))
                {
                    nodata++;
                    continue;
                }
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        if (float.IsPositiveInfinity(min)) min = 0f;
        if (float.IsNegativeInfinity(max)) max = 0f;

        return new MeshQualityReport
        {
            VertexCount = mesh.Vertices.Count,
            TriangleCount = mesh.Indices.Count / 3,
            MinHeightMeters = min,
            MaxHeightMeters = max,
            NoDataSamples = nodata
        };
    }
}

