namespace Nexo.GeoTerrain.Projection;

/// <summary>
/// Lambert Conformal Conic projection (standard parallel-based).
/// Common for regional mapping, especially in North America.
/// </summary>
public sealed class LambertConformalConicProjector : ICoordinateProjector
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

    public LambertConformalConicProjector(
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

    public string ProjectionId => "lambert_conformal_conic";

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
        var (e0, n0) = LatLonToLambert(origin.Latitude.Degrees, origin.Longitude.Degrees);
        var (e1, n1) = LatLonToLambert(point.Latitude.Degrees, point.Longitude.Degrees);
        return new Vector2((float)(e1 - e0), (float)(n1 - n0));
    }

    private (double easting, double northing) LatLonToLambert(double latDeg, double lonDeg)
    {
        var latRad = latDeg * (Math.PI / 180.0);
        var lonRad = lonDeg * (Math.PI / 180.0);
        var lon0Rad = _centralMeridian * (Math.PI / 180.0);
        var lat0Rad = _latitudeOfOrigin * (Math.PI / 180.0);

        var phi1 = _firstStandardParallel * (Math.PI / 180.0);
        var phi2 = _secondStandardParallel * (Math.PI / 180.0);

        var m1 = ComputeM(phi1);
        var m2 = ComputeM(phi2);
        var t1 = ComputeT(phi1);
        var t2 = ComputeT(phi2);
        var t0 = ComputeT(lat0Rad);

        var n = (Math.Log(m1) - Math.Log(m2)) / (Math.Log(t1) - Math.Log(t2));
        var F = m1 / (n * Math.Pow(t1, n));
        var r0 = a * F * Math.Pow(t0, n);

        var t = ComputeT(latRad);
        var r = a * F * Math.Pow(t, n);
        var theta = n * (lonRad - lon0Rad);

        var easting = _falseEasting + r * Math.Sin(theta);
        var northing = _falseNorthing + r0 - r * Math.Cos(theta);

        return (easting, northing);
    }

    private static double ComputeM(double phi)
    {
        var sinPhi = Math.Sin(phi);
        return Math.Cos(phi) / Math.Sqrt(1.0 - e * e * sinPhi * sinPhi);
    }

    private static double ComputeT(double phi)
    {
        var sinPhi = Math.Sin(phi);
        var esinPhi = e * sinPhi;
        return Math.Tan(Math.PI / 4.0 - phi / 2.0) / Math.Pow((1.0 - esinPhi) / (1.0 + esinPhi), e / 2.0);
    }
}
