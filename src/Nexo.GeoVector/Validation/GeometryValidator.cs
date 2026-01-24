using Nexo.GeoVector.Geometry;

namespace Nexo.GeoVector.Validation;

/// <summary>
/// Validates geometric features for integrity (self-intersections, invalid geometry, etc.).
/// </summary>
public static class GeometryValidator
{
    /// <summary>
    /// Validates a polygon for common issues:
    /// - Non-closed rings
    /// - Self-intersections
    /// - Degenerate triangles (collinear points)
    /// </summary>
    public static GeometryValidationResult ValidatePolygon(GeoPolygon polygon)
    {
        if (polygon == null)
            throw new ArgumentNullException(nameof(polygon));

        var issues = new List<string>();

        var points = polygon.Points;
        if (points == null || points.Count < 3)
        {
            issues.Add("Polygon has fewer than 3 points");
            return new GeometryValidationResult { IsValid = false, Issues = issues };
        }

        // Check if polygon is closed
        var first = points[0];
        var last = points[^1];
        if (Math.Abs(first.Latitude.Degrees - last.Latitude.Degrees) > 1e-9 ||
            Math.Abs(first.Longitude.Degrees - last.Longitude.Degrees) > 1e-9)
        {
            issues.Add("Polygon ring is not closed (first and last points differ)");
        }

        // Check for degenerate triangles (three collinear points)
        for (var i = 0; i < points.Count - 2; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[i + 2];

            // Check if three points are collinear (simplified check using cross product)
            var dx1 = p2.Longitude.Degrees - p1.Longitude.Degrees;
            var dy1 = p2.Latitude.Degrees - p1.Latitude.Degrees;
            var dx2 = p3.Longitude.Degrees - p2.Longitude.Degrees;
            var dy2 = p3.Latitude.Degrees - p2.Latitude.Degrees;

            var cross = dx1 * dy2 - dy1 * dx2;
            if (Math.Abs(cross) < 1e-9)
            {
                issues.Add($"Degenerate triangle detected at index {i} (collinear points)");
            }
        }

        // Basic self-intersection check (simplified - checks for obvious cases)
        // Full self-intersection detection would require more complex algorithms
        for (var i = 0; i < points.Count - 1; i++)
        {
            for (var j = i + 2; j < points.Count - 1; j++)
            {
                if (i == 0 && j == points.Count - 2) continue; // Skip adjacent segments

                var seg1Start = points[i];
                var seg1End = points[i + 1];
                var seg2Start = points[j];
                var seg2End = points[j + 1];

                if (SegmentsIntersect(seg1Start, seg1End, seg2Start, seg2End))
                {
                    issues.Add($"Self-intersection detected between segments [{i}-{i + 1}] and [{j}-{j + 1}]");
                }
            }
        }

        return new GeometryValidationResult
        {
            IsValid = issues.Count == 0,
            Issues = issues
        };
    }

    private static bool SegmentsIntersect(GeoPoint p1, GeoPoint p2, GeoPoint p3, GeoPoint p4)
    {
        // Simplified segment intersection check using orientation
        var o1 = Orientation(p1, p2, p3);
        var o2 = Orientation(p1, p2, p4);
        var o3 = Orientation(p3, p4, p1);
        var o4 = Orientation(p3, p4, p2);

        if (o1 != o2 && o3 != o4) return true;
        if (o1 == 0 && OnSegment(p1, p3, p2)) return true;
        if (o2 == 0 && OnSegment(p1, p4, p2)) return true;
        if (o3 == 0 && OnSegment(p3, p1, p4)) return true;
        if (o4 == 0 && OnSegment(p3, p2, p4)) return true;

        return false;
    }

    private static int Orientation(GeoPoint p, GeoPoint q, GeoPoint r)
    {
        var val = (q.Latitude.Degrees - p.Latitude.Degrees) * (r.Longitude.Degrees - q.Longitude.Degrees) -
                  (q.Longitude.Degrees - p.Longitude.Degrees) * (r.Latitude.Degrees - q.Latitude.Degrees);
        if (Math.Abs(val) < 1e-9) return 0;
        return val > 0 ? 1 : 2;
    }

    private static bool OnSegment(GeoPoint p, GeoPoint q, GeoPoint r)
    {
        return q.Longitude.Degrees <= Math.Max(p.Longitude.Degrees, r.Longitude.Degrees) &&
               q.Longitude.Degrees >= Math.Min(p.Longitude.Degrees, r.Longitude.Degrees) &&
               q.Latitude.Degrees <= Math.Max(p.Latitude.Degrees, r.Latitude.Degrees) &&
               q.Latitude.Degrees >= Math.Min(p.Latitude.Degrees, r.Latitude.Degrees);
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
