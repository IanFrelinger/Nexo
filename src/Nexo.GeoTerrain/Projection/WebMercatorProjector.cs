namespace Nexo.GeoTerrain.Projection;

/// <summary>
/// WebMercator (EPSG:3857) projection into meters, then re-based around origin.
/// Useful when pairing geometry with web-mercator raster/vector tiles.
/// </summary>
public sealed class WebMercatorProjector : ICoordinateProjector
{
    public static readonly WebMercatorProjector Instance = new();

    // WGS84 spherical mercator radius used by EPSG:3857
    private const double R = 6378137.0;

    public string ProjectionId => "webmercator";

    public IReadOnlyDictionary<string, string>? Parameters => null;

    public Vector2 ProjectMeters(GeoPoint point, GeoPoint origin)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(point);
        ArgumentNullException.ThrowIfNull(origin);
#else
        if (point is null) throw new ArgumentNullException(nameof(point));
        if (origin is null) throw new ArgumentNullException(nameof(origin));
#endif
        var (x0, y0) = LonLatToMeters(origin.Longitude.Degrees, origin.Latitude.Degrees);
        var (x1, y1) = LonLatToMeters(point.Longitude.Degrees, point.Latitude.Degrees);
        return new Vector2((float)(x1 - x0), (float)(y1 - y0));
    }

    private static (double x, double y) LonLatToMeters(double lonDeg, double latDeg)
    {
        // Clamp latitude to mercator limits.
        var maxLat = 85.05112878;
        if (latDeg > maxLat) latDeg = maxLat;
        if (latDeg < -maxLat) latDeg = -maxLat;

        var lonRad = lonDeg * (Math.PI / 180.0);
        var latRad = latDeg * (Math.PI / 180.0);
        var x = R * lonRad;
        var y = R * Math.Log(Math.Tan((Math.PI / 4.0) + (latRad / 2.0)));
        return (x, y);
    }
}

