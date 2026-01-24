using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Geometry;

namespace Nexo.Adapters.GeoVector.Validation;

/// <summary>
/// Validates vector geometry for integrity issues.
/// Detects self-intersecting polygons, invalid geometry, and degenerate triangles.
/// </summary>
public static class GeometryValidator
{
    /// <summary>
    /// Validates a polygon for common geometry issues.
    /// </summary>
    public static GeometryValidationResult ValidatePolygon(GeoPolygon polygon, ILogger? logger = null)
    {
        if (polygon == null)
            throw new ArgumentNullException(nameof(polygon));

        var issues = new List<string>();

        // Check minimum point count
        if (polygon.Points.Count < 3)
        {
            issues.Add("Polygon must have at least 3 points");
        }

        // Check if polygon is closed
        if (polygon.Points.Count > 0)
        {
            var first = polygon.Points[0];
            var last = polygon.Points[^1];
            if (Math.Abs(first.Latitude.Degrees - last.Latitude.Degrees) > 1e-9 ||
                Math.Abs(first.Longitude.Degrees - last.Longitude.Degrees) > 1e-9)
            {
                issues.Add("Polygon is not closed (first and last points differ)");
            }
        }

        // Check for duplicate consecutive points
        for (var i = 0; i < polygon.Points.Count - 1; i++)
        {
            var p1 = polygon.Points[i];
            var p2 = polygon.Points[i + 1];
            if (Math.Abs(p1.Latitude.Degrees - p2.Latitude.Degrees) < 1e-9 &&
                Math.Abs(p1.Longitude.Degrees - p2.Longitude.Degrees) < 1e-9)
            {
                issues.Add($"Duplicate consecutive points at index {i}");
            }
        }

        // Check for self-intersection (simplified - checks for obvious cases)
        if (polygon.Points.Count >= 4)
        {
            var hasSelfIntersection = CheckSelfIntersection(polygon);
            if (hasSelfIntersection)
            {
                issues.Add("Polygon appears to be self-intersecting");
            }
        }

        // Check for degenerate triangles (if polygon is a triangle)
        if (polygon.Points.Count == 4) // 3 points + closing point
        {
            var p0 = polygon.Points[0];
            var p1 = polygon.Points[1];
            var p2 = polygon.Points[2];

            // Check if all points are collinear
            var area = CalculateTriangleArea(p0, p1, p2);
            if (Math.Abs(area) < 1e-10)
            {
                issues.Add("Degenerate triangle (all points collinear)");
            }
        }

        var isValid = issues.Count == 0;
        if (!isValid && logger != null)
        {
            logger.LogWarning("Polygon validation failed: {Issues}", string.Join("; ", issues));
        }

        return new GeometryValidationResult
        {
            IsValid = isValid,
            Issues = issues
        };
    }

    /// <summary>
    /// Validates a polyline for common geometry issues.
    /// </summary>
    public static GeometryValidationResult ValidatePolyline(GeoPolyline polyline, ILogger? logger = null)
    {
        if (polyline == null)
            throw new ArgumentNullException(nameof(polyline));

        var issues = new List<string>();

        // Check minimum point count
        if (polyline.Points.Count < 2)
        {
            issues.Add("Polyline must have at least 2 points");
        }

        // Check for duplicate consecutive points
        for (var i = 0; i < polyline.Points.Count - 1; i++)
        {
            var p1 = polyline.Points[i];
            var p2 = polyline.Points[i + 1];
            if (Math.Abs(p1.Latitude.Degrees - p2.Latitude.Degrees) < 1e-9 &&
                Math.Abs(p1.Longitude.Degrees - p2.Longitude.Degrees) < 1e-9)
            {
                issues.Add($"Duplicate consecutive points at index {i}");
            }
        }

        var isValid = issues.Count == 0;
        if (!isValid && logger != null)
        {
            logger.LogWarning("Polyline validation failed: {Issues}", string.Join("; ", issues));
        }

        return new GeometryValidationResult
        {
            IsValid = isValid,
            Issues = issues
        };
    }

    private static bool CheckSelfIntersection(GeoPolygon polygon)
    {
        // Simplified self-intersection check using line segment intersection
        // This is a basic implementation - for production, use a proper algorithm
        var points = polygon.Points;
        var n = points.Count;

        for (var i = 0; i < n - 1; i++)
        {
            var seg1Start = points[i];
            var seg1End = points[i + 1];

            for (var j = i + 2; j < n - 1; j++)
            {
                // Skip adjacent segments
                if (j == i + 1 || (i == 0 && j == n - 2))
                    continue;

                var seg2Start = points[j];
                var seg2End = points[j + 1];

                if (DoSegmentsIntersect(seg1Start, seg1End, seg2Start, seg2End))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool DoSegmentsIntersect(GeoPoint p1, GeoPoint p2, GeoPoint p3, GeoPoint p4)
    {
        // Simplified 2D line segment intersection check
        var o1 = Orientation(p1, p2, p3);
        var o2 = Orientation(p1, p2, p4);
        var o3 = Orientation(p3, p4, p1);
        var o4 = Orientation(p3, p4, p2);

        if (o1 != o2 && o3 != o4)
            return true;

        return false;
    }

    private static int Orientation(GeoPoint p1, GeoPoint p2, GeoPoint p3)
    {
        var val = (p2.Longitude.Degrees - p1.Longitude.Degrees) * (p3.Latitude.Degrees - p2.Latitude.Degrees) -
                  (p2.Latitude.Degrees - p1.Latitude.Degrees) * (p3.Longitude.Degrees - p2.Longitude.Degrees);
        return val > 0 ? 1 : (val < 0 ? -1 : 0);
    }

    private static double CalculateTriangleArea(GeoPoint p1, GeoPoint p2, GeoPoint p3)
    {
        return Math.Abs((p1.Longitude.Degrees * (p2.Latitude.Degrees - p3.Latitude.Degrees) +
                        p2.Longitude.Degrees * (p3.Latitude.Degrees - p1.Latitude.Degrees) +
                        p3.Longitude.Degrees * (p1.Latitude.Degrees - p2.Latitude.Degrees)) / 2.0);
    }
}

/// <summary>
/// Result of geometry validation.
/// </summary>
public sealed record GeometryValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<string> Issues { get; init; }
}
