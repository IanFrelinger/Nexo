namespace Nexo.GeoTerrain;

/// <summary>
/// Computes comprehensive quality metrics for an elevation grid and/or a generated mesh.
/// Includes triangle quality, accuracy validation, and slope analysis.
/// </summary>
public static class MeshQualityAnalyzer
{
    public static MeshQualityReport Analyze(ElevationGrid grid, MeshData mesh, bool includeAdvancedMetrics = true)
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

        var report = new MeshQualityReport
        {
            VertexCount = mesh.Vertices.Count,
            TriangleCount = mesh.Indices.Count / 3,
            MinHeightMeters = min,
            MaxHeightMeters = max,
            NoDataSamples = nodata
        };

        if (includeAdvancedMetrics)
        {
            report = AnalyzeTriangleQuality(report, mesh);
            report = AnalyzeMeshAccuracy(report, grid, mesh);
            report = AnalyzeSlopes(report, mesh);
        }

        return report;
    }

    private static MeshQualityReport AnalyzeTriangleQuality(MeshQualityReport report, MeshData mesh)
    {
        var aspectRatios = new List<float>();
        var areas = new List<float>();
        var edgeLengths = new List<float>();
        var thinCount = 0;

        for (var i = 0; i < mesh.Indices.Count; i += 3)
        {
            var v0 = mesh.Vertices[mesh.Indices[i]];
            var v1 = mesh.Vertices[mesh.Indices[i + 1]];
            var v2 = mesh.Vertices[mesh.Indices[i + 2]];

            var e1 = v1 - v0;
            var e2 = v2 - v0;
            var e3 = v2 - v1;

            var len1 = e1.Length();
            var len2 = e2.Length();
            var len3 = e3.Length();

            edgeLengths.Add(len1);
            edgeLengths.Add(len2);
            edgeLengths.Add(len3);

            var area = Vector3.Cross(e1, e2).Length() * 0.5f;
            areas.Add(area);

            // Aspect ratio: ratio of longest edge to shortest edge
            var maxEdge = Math.Max(len1, Math.Max(len2, len3));
            var minEdge = Math.Min(len1, Math.Min(len2, len3));
            var aspectRatio = minEdge > 0 ? maxEdge / minEdge : float.PositiveInfinity;
            aspectRatios.Add(aspectRatio);

            if (aspectRatio > 10.0f) thinCount++;
        }

        // Calculate variance of edge lengths
        var avgEdgeLength = edgeLengths.Average();
        var edgeVariance = edgeLengths.Sum(l => (l - avgEdgeLength) * (l - avgEdgeLength)) / edgeLengths.Count;

        return report with
        {
            MinAspectRatio = aspectRatios.Count > 0 ? aspectRatios.Min() : null,
            MaxAspectRatio = aspectRatios.Count > 0 ? aspectRatios.Max() : null,
            AverageAspectRatio = aspectRatios.Count > 0 ? aspectRatios.Average() : null,
            ThinTriangleCount = thinCount,
            MinTriangleArea = areas.Count > 0 ? areas.Min() : null,
            MaxTriangleArea = areas.Count > 0 ? areas.Max() : null,
            AverageTriangleArea = areas.Count > 0 ? areas.Average() : null,
            EdgeLengthVariance = edgeVariance
        };
    }

    private static MeshQualityReport AnalyzeMeshAccuracy(MeshQualityReport report, ElevationGrid grid, MeshData mesh)
    {
        var deviations = new List<float>();
        var errors = new List<float>();

        // Sample mesh vertices and compare to source grid
        foreach (var vertex in mesh.Vertices)
        {
            // Convert vertex position back to grid coordinates
            var x = (int)Math.Round(vertex.X / grid.Spacing.MetersX);
            var y = (int)Math.Round(vertex.Z / grid.Spacing.MetersY);

            if (x >= 0 && x < grid.Width && y >= 0 && y < grid.Height)
            {
                var gridHeight = grid.GetHeightMeters(x, y);
                if (!float.IsNaN(gridHeight))
                {
                    var deviation = Math.Abs(vertex.Y - gridHeight);
                    deviations.Add(deviation);
                    errors.Add(vertex.Y - gridHeight);
                }
            }
        }

        if (deviations.Count == 0)
            return report;

        var maxDev = deviations.Max();
        var meanError = errors.Average();
        var rmsError = Math.Sqrt(errors.Sum(e => e * e) / errors.Count);

        return report with
        {
            MaxDeviationMeters = maxDev,
            MeanErrorMeters = (float)meanError,
            RmsErrorMeters = (float)rmsError
        };
    }

    private static MeshQualityReport AnalyzeSlopes(MeshQualityReport report, MeshData mesh)
    {
        var slopes = new List<float>();
        var steepCount = 0;

        for (var i = 0; i < mesh.Indices.Count; i += 3)
        {
            var v0 = mesh.Vertices[mesh.Indices[i]];
            var v1 = mesh.Vertices[mesh.Indices[i + 1]];
            var v2 = mesh.Vertices[mesh.Indices[i + 2]];

            // Calculate triangle normal
            var e1 = v1 - v0;
            var e2 = v2 - v0;
            var normal = Vector3.Cross(e1, e2);
            var normalLength = normal.Length();
            if (normalLength < 1e-6f) continue;

            normal = Vector3.Multiply(normal, 1.0f / normalLength);

            // Slope is angle between normal and vertical (0,1,0)
            var vertical = new Vector3(0, 1, 0);
            var dot = Math.Max(-1.0f, Math.Min(1.0f, Vector3.Dot(normal, vertical)));
            var slopeRad = Math.Acos(dot);
            var slopeDeg = (float)(slopeRad * 180.0 / Math.PI);

            slopes.Add(slopeDeg);
            if (slopeDeg > 45.0f) steepCount++;
        }

        return report with
        {
            MaxSlopeDegrees = slopes.Count > 0 ? slopes.Max() : null,
            SteepSlopeCount = steepCount
        };
    }
}

