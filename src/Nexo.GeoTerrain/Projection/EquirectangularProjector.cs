namespace Nexo.GeoTerrain.Projection;

/// <summary>
/// Fast local tangent-plane approximation. Good for small-ish bounds.
/// </summary>
public sealed class EquirectangularProjector : ICoordinateProjector
{
    public static readonly EquirectangularProjector Instance = new();

    public string ProjectionId => "local_equirectangular";

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
        // meters per degree latitude ~ 111,320
        // meters per degree longitude ~ 111,320 * cos(lat0)
        var lat0Rad = origin.Latitude.Degrees * (Math.PI / 180.0);
        var mPerDegLat = 111_320.0;
        var mPerDegLon = 111_320.0 * Math.Cos(lat0Rad);

        var dx = (point.Longitude.Degrees - origin.Longitude.Degrees) * mPerDegLon;
        var dy = (point.Latitude.Degrees - origin.Latitude.Degrees) * mPerDegLat;
        return new Vector2((float)dx, (float)dy);
    }
}

