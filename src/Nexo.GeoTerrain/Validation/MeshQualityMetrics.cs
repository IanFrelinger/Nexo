
namespace Nexo.GeoTerrain.Validation;

/// <summary>
/// Advanced mesh quality metrics including triangle quality, accuracy validation, and geometric analysis.
/// </summary>
public static class MeshQualityMetrics
{
    /// <summary>
    /// Computes triangle quality metrics including aspect ratio, area distribution, and edge length variance.
    /// </summary>
    public static TriangleQualityMetrics ComputeTriangleQuality(MeshData mesh)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(mesh);
#else
        if (mesh == null)
            throw new ArgumentNullException(nameof(mesh));
#endif
        if (mesh.Vertices == null || mesh.Indices == null)
            throw new ArgumentException("Mesh must have vertices and indices.", nameof(mesh));

        var aspectRatios = new List<float>();
        var areas = new List<float>();
        var edgeLengths = new List<float>();
        var minAspectRatio = float.PositiveInfinity;
        var maxAspectRatio = float.NegativeInfinity;
        var minArea = float.PositiveInfinity;
        var maxArea = float.NegativeInfinity;

        for (var i = 0; i < mesh.Indices.Count; i += 3)
        {
            var i0 = mesh.Indices[i];
            var i1 = mesh.Indices[i + 1];
            var i2 = mesh.Indices[i + 2];

            var v0 = mesh.Vertices[i0];
            var v1 = mesh.Vertices[i1];
            var v2 = mesh.Vertices[i2];

            // Compute edge lengths
            var e0 = Distance(v1, v2);
            var e1 = Distance(v2, v0);
            var e2 = Distance(v0, v1);

            edgeLengths.Add(e0);
            edgeLengths.Add(e1);
            edgeLengths.Add(e2);

            // Compute area (using cross product)
            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var area = Vector3.Cross(edge1, edge2).Length() * 0.5f;
            areas.Add(area);

            if (area < minArea) minArea = area;
            if (area > maxArea) maxArea = area;

            // Compute aspect ratio (longest edge / shortest edge)
            var maxEdge = Math.Max(Math.Max(e0, e1), e2);
            var minEdge = Math.Min(Math.Min(e0, e1), e2);
            var aspectRatio = minEdge > 0 ? maxEdge / minEdge : float.PositiveInfinity;
            aspectRatios.Add(aspectRatio);

            if (aspectRatio < minAspectRatio) minAspectRatio = aspectRatio;
            if (aspectRatio > maxAspectRatio) maxAspectRatio = aspectRatio;
        }

        // Compute statistics
        var avgAspectRatio = aspectRatios.Count > 0 ? aspectRatios.Average() : 0f;
        var avgArea = areas.Count > 0 ? areas.Average() : 0f;
        var areaVariance = areas.Count > 0 ? ComputeVariance(areas, avgArea) : 0f;

        // Edge length variance
        var avgEdgeLength = edgeLengths.Count > 0 ? edgeLengths.Average() : 0f;
        var edgeLengthVariance = edgeLengths.Count > 0 ? ComputeVariance(edgeLengths, avgEdgeLength) : 0f;

        // Count sliver triangles (aspect ratio > 10)
        var sliverCount = aspectRatios.Count(r => r > 10f);

        return new TriangleQualityMetrics
        {
            MinAspectRatio = minAspectRatio == float.PositiveInfinity ? 0f : minAspectRatio,
            MaxAspectRatio = maxAspectRatio == float.NegativeInfinity ? 0f : maxAspectRatio,
            AverageAspectRatio = avgAspectRatio,
            MinArea = minArea == float.PositiveInfinity ? 0f : minArea,
            MaxArea = maxArea == float.NegativeInfinity ? 0f : maxArea,
            AverageArea = avgArea,
            AreaVariance = areaVariance,
            AverageEdgeLength = avgEdgeLength,
            EdgeLengthVariance = edgeLengthVariance,
            SliverTriangleCount = sliverCount,
            SliverTriangleRatio = aspectRatios.Count > 0 ? (float)sliverCount / aspectRatios.Count : 0f
        };
    }

    /// <summary>
    /// Validates mesh accuracy by comparing generated mesh to source elevation grid.
    /// Computes deviation, maximum error, and RMS error.
    /// </summary>
    public static MeshAccuracyReport ValidateMeshAccuracy(MeshData mesh, ElevationGrid sourceGrid)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(sourceGrid);
#else
        if (mesh == null)
            throw new ArgumentNullException(nameof(mesh));
        if (sourceGrid == null)
            throw new ArgumentNullException(nameof(sourceGrid));
#endif
        if (mesh.Vertices == null)
            throw new ArgumentException("Mesh must have vertices.", nameof(mesh));

        var errors = new List<float>();
        var maxError = 0f;
        var totalErrorSquared = 0.0;

        // Sample mesh vertices and compare to grid
        foreach (var vertex in mesh.Vertices)
        {
            // Convert vertex position to grid coordinates
            // Note: This is a simplified approach - actual implementation would depend on projection
            var lat = (float)vertex.Y; // Assuming Y is latitude
            var lon = (float)vertex.X; // Assuming X is longitude

            // Check if within bounds
            if (!sourceGrid.Bounds.Contains(new GeoPoint
            {
                Latitude = new Latitude(lat),
                Longitude = new Longitude(lon)
            }))
            {
                continue;
            }

            // Convert spacing from meters to approximate degrees
            // Rough conversion: 1 degree latitude ≈ 111320 meters
            var spacingDegreesX = sourceGrid.Spacing.MetersX / 111320.0;
            var spacingDegreesY = sourceGrid.Spacing.MetersY / 111320.0;
            
            // Get expected elevation from grid
            var gridX = (int)((lon - sourceGrid.Bounds.MinLongitude.Degrees) / spacingDegreesX);
            var gridY = (int)((lat - sourceGrid.Bounds.MinLatitude.Degrees) / spacingDegreesY);

            if (gridX >= 0 && gridX < sourceGrid.Width && gridY >= 0 && gridY < sourceGrid.Height)
            {
                var expectedElevation = sourceGrid.GetHeightMeters(gridX, gridY);
                if (!float.IsNaN(expectedElevation))
                {
                    var actualElevation = vertex.Z; // Assuming Z is elevation
                    var error = Math.Abs(actualElevation - expectedElevation);
                    errors.Add(error);
                    totalErrorSquared += error * error;

                    if (error > maxError)
                        maxError = error;
                }
            }
        }

        var rmsError = errors.Count > 0 ? Math.Sqrt(totalErrorSquared / errors.Count) : 0.0;
        var averageError = errors.Count > 0 ? errors.Average() : 0f;

        return new MeshAccuracyReport
        {
            AverageDeviation = averageError,
            MaxDeviation = maxError,
            RmsError = (float)rmsError,
            SampleCount = errors.Count,
            MaxAcceptableError = 10f // 10 meters default tolerance
        };
    }

    /// <summary>
    /// Validates maximum slope in the mesh.
    /// </summary>
    public static MaxSlopeReport ValidateMaxSlope(MeshData mesh, float maxAllowedSlopeDegrees = 60f)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(mesh);
#else
        if (mesh == null)
            throw new ArgumentNullException(nameof(mesh));
#endif
        if (mesh.Vertices == null || mesh.Indices == null)
            throw new ArgumentException("Mesh must have vertices and indices.", nameof(mesh));

        var slopes = new List<float>();
        var maxSlope = 0f;
        var steepTriangleCount = 0;

        for (var i = 0; i < mesh.Indices.Count; i += 3)
        {
            var i0 = mesh.Indices[i];
            var i1 = mesh.Indices[i + 1];
            var i2 = mesh.Indices[i + 2];

            var v0 = mesh.Vertices[i0];
            var v1 = mesh.Vertices[i1];
            var v2 = mesh.Vertices[i2];

            // Compute triangle normal
                var edge1 = v1 - v0;
                var edge2 = v2 - v0;
                var normal = Vector3.Cross(edge1, edge2);
                var area = normal.Length() * 0.5f;

                if (area > 0)
                {
                    normal = normal.Normalized();

                    // Compute slope as angle from vertical (Z-axis)
                    var vertical = new Vector3(0, 0, 1);
                    var dot = Vector3.Dot(normal, vertical);
                    var angleRadians = Math.Acos(Clamp(dot, -1f, 1f));
                    var angleDegrees = (float)(angleRadians * 180.0 / Math.PI);
                    var slope = 90f - angleDegrees; // Slope from horizontal

                slopes.Add(slope);
                if (slope > maxSlope)
                    maxSlope = slope;

                if (slope > maxAllowedSlopeDegrees)
                    steepTriangleCount++;
            }
        }

        return new MaxSlopeReport
        {
            MaxSlopeDegrees = maxSlope,
            AverageSlopeDegrees = slopes.Count > 0 ? slopes.Average() : 0f,
            SteepTriangleCount = steepTriangleCount,
            SteepTriangleRatio = slopes.Count > 0 ? (float)steepTriangleCount / slopes.Count : 0f,
            MaxAllowedSlopeDegrees = maxAllowedSlopeDegrees,
            IsValid = maxSlope <= maxAllowedSlopeDegrees
        };
    }

    /// <summary>
    /// Computes normal consistency across the mesh.
    /// </summary>
    public static NormalConsistencyReport ComputeNormalConsistency(MeshData mesh)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(mesh);
#else
        if (mesh == null)
            throw new ArgumentNullException(nameof(mesh));
#endif
        if (mesh.Vertices == null || mesh.Indices == null)
            throw new ArgumentException("Mesh must have vertices and indices.", nameof(mesh));

        // Build vertex-to-triangle mapping
        var vertexTriangles = new Dictionary<int, List<int>>();
        for (var i = 0; i < mesh.Indices.Count; i += 3)
        {
            var tri = i / 3;
            for (var j = 0; j < 3; j++)
            {
                var vid = mesh.Indices[i + j];
                if (!vertexTriangles.ContainsKey(vid))
                    vertexTriangles[vid] = new List<int>();
                vertexTriangles[vid].Add(tri);
            }
        }

        var inconsistencies = 0;
        var totalComparisons = 0;

        // For each vertex, compare normals of adjacent triangles
        foreach (var kvp in vertexTriangles)
        {
            var triangles = kvp.Value;
            if (triangles.Count < 2)
                continue;

            // Compute normals for each triangle
            var normals = new List<Vector3>();
            foreach (var tri in triangles)
            {
                var idx = tri * 3;
                var i0 = mesh.Indices[idx];
                var i1 = mesh.Indices[idx + 1];
                var i2 = mesh.Indices[idx + 2];

                var v0 = mesh.Vertices[i0];
                var v1 = mesh.Vertices[i1];
                var v2 = mesh.Vertices[i2];

                var edge1 = v1 - v0;
                var edge2 = v2 - v0;
                var normal = Vector3.Cross(edge1, edge2);
                if (normal.Length() > 0)
                    normal = normal.Normalized();
                normals.Add(normal);
            }

            // Check consistency between normals
            for (var i = 0; i < normals.Count; i++)
            {
                for (var j = i + 1; j < normals.Count; j++)
                {
                    totalComparisons++;
                    var dot = Vector3.Dot(normals[i], normals[j]);
                    // If normals point in opposite directions (dot < -0.5), inconsistent
                    if (dot < -0.5f)
                        inconsistencies++;
                }
            }
        }

        var consistencyRatio = totalComparisons > 0 ? 1f - ((float)inconsistencies / totalComparisons) : 1f;

        return new NormalConsistencyReport
        {
            ConsistencyRatio = consistencyRatio,
            InconsistencyCount = inconsistencies,
            TotalComparisons = totalComparisons
        };
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private static float Distance(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var dz = a.Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static float ComputeVariance(IReadOnlyList<float> values, float mean)
    {
        if (values.Count == 0) return 0f;
        var sumSquaredDiff = values.Sum(v => (v - mean) * (v - mean));
        return sumSquaredDiff / values.Count;
    }
}

/// <summary>
/// Triangle quality metrics.
/// </summary>
public sealed record TriangleQualityMetrics
{
    public required float MinAspectRatio { get; init; }
    public required float MaxAspectRatio { get; init; }
    public required float AverageAspectRatio { get; init; }
    public required float MinArea { get; init; }
    public required float MaxArea { get; init; }
    public required float AverageArea { get; init; }
    public required float AreaVariance { get; init; }
    public required float AverageEdgeLength { get; init; }
    public required float EdgeLengthVariance { get; init; }
    public required int SliverTriangleCount { get; init; }
    public required float SliverTriangleRatio { get; init; }
}

/// <summary>
/// Mesh accuracy validation report.
/// </summary>
public sealed record MeshAccuracyReport
{
    public required float AverageDeviation { get; init; }
    public required float MaxDeviation { get; init; }
    public required float RmsError { get; init; }
    public required int SampleCount { get; init; }
    public required float MaxAcceptableError { get; init; }
    public bool IsAcceptable => MaxDeviation <= MaxAcceptableError;
}

/// <summary>
/// Maximum slope validation report.
/// </summary>
public sealed record MaxSlopeReport
{
    public required float MaxSlopeDegrees { get; init; }
    public required float AverageSlopeDegrees { get; init; }
    public required int SteepTriangleCount { get; init; }
    public required float SteepTriangleRatio { get; init; }
    public required float MaxAllowedSlopeDegrees { get; init; }
    public required bool IsValid { get; init; }
}

/// <summary>
/// Normal consistency report.
/// </summary>
public sealed record NormalConsistencyReport
{
    public required float ConsistencyRatio { get; init; }
    public required int InconsistencyCount { get; init; }
    public required int TotalComparisons { get; init; }
}
