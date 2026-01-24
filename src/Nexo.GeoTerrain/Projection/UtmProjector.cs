namespace Nexo.GeoTerrain.Projection;

/// <summary>
/// UTM forward projection (WGS84) into meters, re-based around origin.
/// Deterministic and dependency-free; intended for larger bounds than local equirectangular.
/// </summary>
public sealed class UtmProjector : ICoordinateProjector
{
    private readonly int _zone;
    private readonly bool _northernHemisphere;
    private readonly IReadOnlyDictionary<string, string> _parameters;

    public UtmProjector(int zone, bool northernHemisphere)
    {
        if (zone < 1 || zone > 60) throw new ArgumentOutOfRangeException(nameof(zone), zone, "UTM zone must be 1..60.");
        _zone = zone;
        _northernHemisphere = northernHemisphere;
        _parameters = new Dictionary<string, string>
        {
            ["utmZone"] = zone.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["hemisphere"] = northernHemisphere ? "N" : "S"
        };
    }

    public string ProjectionId => "utm";

    public IReadOnlyDictionary<string, string>? Parameters => _parameters;

    public Vector2 ProjectMeters(GeoPoint point, GeoPoint origin)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(point);
        ArgumentNullException.ThrowIfNull(origin);
#else
        if (point is null) throw new ArgumentNullException(nameof(point));
        if (origin is null) throw new ArgumentNullException(nameof(origin));
#endif
        var (e0, n0) = LatLonToUtm(origin.Latitude.Degrees, origin.Longitude.Degrees, _zone, _northernHemisphere);
        var (e1, n1) = LatLonToUtm(point.Latitude.Degrees, point.Longitude.Degrees, _zone, _northernHemisphere);
        return new Vector2((float)(e1 - e0), (float)(n1 - n0));
    }

    public static UtmProjector CreateForBounds(GeoBounds bounds)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(bounds);
#else
        if (bounds is null) throw new ArgumentNullException(nameof(bounds));
#endif
        bounds.Validate();
        var midLat = (bounds.MinLatitude.Degrees + bounds.MaxLatitude.Degrees) * 0.5;
        var midLon = (bounds.MinLongitude.Degrees + bounds.MaxLongitude.Degrees) * 0.5;
        var zone = LonToUtmZone(midLon);
        var north = midLat >= 0;
        return new UtmProjector(zone, north);
    }

    private static int LonToUtmZone(double lonDeg)
    {
        // Standard zone formula.
        var z = (int)Math.Floor((lonDeg + 180.0) / 6.0) + 1;
        if (z < 1) z = 1;
        if (z > 60) z = 60;
        return z;
    }

    // WGS84 constants
    private const double a = 6378137.0;                    // semi-major axis
    private const double f = 1.0 / 298.257223563;          // flattening
    private const double k0 = 0.9996;                      // scale factor

    private static (double easting, double northing) LatLonToUtm(double latDeg, double lonDeg, int zone, bool northernHemisphere)
    {
        // Implementation based on standard UTM forward equations (WGS84).
        var latRad = latDeg * (Math.PI / 180.0);
        var lonRad = lonDeg * (Math.PI / 180.0);

        var e2 = f * (2.0 - f);               // eccentricity squared
        var ep2 = e2 / (1.0 - e2);            // second eccentricity squared

        var lon0Deg = (zone - 1) * 6.0 - 180.0 + 3.0; // central meridian
        var lon0Rad = lon0Deg * (Math.PI / 180.0);

        var sinLat = Math.Sin(latRad);
        var cosLat = Math.Cos(latRad);
        var tanLat = Math.Tan(latRad);

        var N = a / Math.Sqrt(1.0 - e2 * sinLat * sinLat);
        var T = tanLat * tanLat;
        var C = ep2 * cosLat * cosLat;
        var A = cosLat * (lonRad - lon0Rad);

        // Meridional arc
        var e4 = e2 * e2;
        var e6 = e4 * e2;
        var M = a * ((1.0 - e2 / 4.0 - 3.0 * e4 / 64.0 - 5.0 * e6 / 256.0) * latRad
                     - (3.0 * e2 / 8.0 + 3.0 * e4 / 32.0 + 45.0 * e6 / 1024.0) * Math.Sin(2.0 * latRad)
                     + (15.0 * e4 / 256.0 + 45.0 * e6 / 1024.0) * Math.Sin(4.0 * latRad)
                     - (35.0 * e6 / 3072.0) * Math.Sin(6.0 * latRad));

        var A2 = A * A;
        var A3 = A2 * A;
        var A4 = A3 * A;
        var A5 = A4 * A;
        var A6 = A5 * A;

        var easting = k0 * N * (A
                                + (1.0 - T + C) * A3 / 6.0
                                + (5.0 - 18.0 * T + T * T + 72.0 * C - 58.0 * ep2) * A5 / 120.0)
                      + 500000.0;

        var northing = k0 * (M
                             + N * tanLat * (A2 / 2.0
                                             + (5.0 - T + 9.0 * C + 4.0 * C * C) * A4 / 24.0
                                             + (61.0 - 58.0 * T + T * T + 600.0 * C - 330.0 * ep2) * A6 / 720.0));

        if (!northernHemisphere)
        {
            northing += 10000000.0; // false northing for southern hemisphere
        }

        return (easting, northing);
    }
}

