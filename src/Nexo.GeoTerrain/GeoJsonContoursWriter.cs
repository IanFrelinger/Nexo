using System.Globalization;
using System.Text;

namespace Nexo.GeoTerrain;

/// <summary>
/// Deterministic GeoJSON writer for contour polylines.
/// </summary>
public static class GeoJsonContoursWriter
{
    public static string Write(ElevationGrid grid, IReadOnlyList<ContourLine> contours, bool includeElevation = true)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(contours);
#else
        if (grid is null) throw new ArgumentNullException(nameof(grid));
        if (contours is null) throw new ArgumentNullException(nameof(contours));
#endif

        var totalWidthMeters = (grid.Width - 1) * grid.Spacing.MetersX;
        var totalHeightMeters = (grid.Height - 1) * grid.Spacing.MetersY;
        if (totalWidthMeters <= 0 || totalHeightMeters <= 0)
            throw new InvalidOperationException("ElevationGrid must be at least 2x2 to write contours.");

        var minLon = grid.Bounds.MinLongitude.Degrees;
        var maxLon = grid.Bounds.MaxLongitude.Degrees;
        var minLat = grid.Bounds.MinLatitude.Degrees;
        var maxLat = grid.Bounds.MaxLatitude.Degrees;

        // SRTM convention: first row is the northern edge. Map z=0 => maxLat, z=max => minLat.
        double LonFromX(double x)
        {
            var t = x / totalWidthMeters;
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            return minLon + (maxLon - minLon) * t;
        }

        double LatFromZ(double z)
        {
            var t = z / totalHeightMeters;
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            return maxLat - (maxLat - minLat) * t;
        }

        var sb = new StringBuilder(capacity: Math.Max(1024, contours.Count * 256));
        sb.Append("{\"type\":\"FeatureCollection\",\"features\":[");

        var firstFeature = true;
        for (var i = 0; i < contours.Count; i++)
        {
            var line = contours[i];
            if (line.Points is null || line.Points.Count < 2) continue;

            if (!firstFeature) sb.Append(',');
            firstFeature = false;

            sb.Append("{\"type\":\"Feature\",\"properties\":{");
            sb.Append("\"elevation_m\":").Append(line.ElevationMeters.ToString("G9", CultureInfo.InvariantCulture));
            sb.Append("},\"geometry\":{\"type\":\"LineString\",\"coordinates\":[");

            for (var p = 0; p < line.Points.Count; p++)
            {
                if (p > 0) sb.Append(',');
                var pt = line.Points[p];
                var lon = LonFromX(pt.X);
                var lat = LatFromZ(pt.Z);

                sb.Append('[')
                  .Append(lon.ToString("G17", CultureInfo.InvariantCulture)).Append(',')
                  .Append(lat.ToString("G17", CultureInfo.InvariantCulture));

                if (includeElevation)
                {
                    sb.Append(',').Append(line.ElevationMeters.ToString("G9", CultureInfo.InvariantCulture));
                }

                sb.Append(']');
            }

            sb.Append("]}}");
        }

        sb.Append("]}");
        return sb.ToString();
    }
}

