namespace Nexo.GeoTerrain.Projection;

/// <summary>
/// Albers Equal Area Conic projection.
/// Common for continental mapping, preserves area.
/// </summary>
public sealed class AlbersEqualAreaProjector : ICoordinateProjector
{
    private readonly double _firstStandardParallel;
    private readonly double _secondStandardParallel;
    private readonly double _centralMeridian;
    private readonly double _latitudeOfOrigin;
    private readonly double _falseEasting;
    private readonly double _falseNorthing;
    private readonly IReadOnlyDictionary<string, string> _parameters;

    // WGS84 ellipsoid constants
    private const double a = 6378137.0;                    // semi-major axis
    private const double e = 0.081819190842622;           // eccentricity

    public AlbersEqualAreaProjector(
        double firstStandardParallel,
        double secondStandardParallel,
        double centralMeridian,
        double latitudeOfOrigin,
        double falseEasting = 0.0,
        double falseNorthing = 0.0)
    {
        _firstStandardParallel = firstStandardParallel;
        _secondStandardParallel = secondStandardParallel;
        _centralMeridian = centralMeridian;
        _latitudeOfOrigin = latitudeOfOrigin;
        _falseEasting = falseEasting;
        _falseNorthing = falseNorthing;

        _parameters = new Dictionary<string, string>
        {
            ["firstStandardParallel"] = firstStandardParallel.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["secondStandardParallel"] = secondStandardParallel.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["centralMeridian"] = centralMeridian.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["latitudeOfOrigin"] = latitudeOfOrigin.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    public string ProjectionId => "albers_equal_area";

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
        var (e0, n0) = LatLonToAlbers(origin.Latitude.Degrees, origin.Longitude.Degrees);
        var (e1, n1) = LatLonToAlbers(point.Latitude.Degrees, point.Longitude.Degrees);
        return new Vector2((float)(e1 - e0), (float)(n1 - n0));
    }

    private (double easting, double northing) LatLonToAlbers(double latDeg, double lonDeg)
    {
        var latRad = latDeg * (Math.PI / 180.0);
        var lonRad = lonDeg * (Math.PI / 180.0);
        var lon0Rad = _centralMeridian * (Math.PI / 180.0);
        var lat0Rad = _latitudeOfOrigin * (Math.PI / 180.0);

        var phi1 = _firstStandardParallel * (Math.PI / 180.0);
        var phi2 = _secondStandardParallel * (Math.PI / 180.0);

        var n = (Math.Sin(phi1) + Math.Sin(phi2)) / 2.0;
        var C = ComputeC(phi1) + ComputeC(phi2);
        var rho0 = a * Math.Sqrt(C - ComputeC(lat0Rad)) / n;
        var theta = n * (lonRad - lon0Rad);

        var rho = a * Math.Sqrt(C - ComputeC(latRad)) / n;

        var easting = _falseEasting + rho * Math.Sin(theta);
        var northing = _falseNorthing + rho0 - rho * Math.Cos(theta);

        return (easting, northing);
    }

    private static double ComputeC(double phi)
    {
        var sinPhi = Math.Sin(phi);
        var esinPhi = e * sinPhi;
        return (1.0 - e * e) * ((sinPhi / (1.0 - esinPhi * esinPhi)) - (1.0 / (2.0 * e)) * Math.Log((1.0 - esinPhi) / (1.0 + esinPhi)));
    }
}
