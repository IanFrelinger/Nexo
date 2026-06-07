namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Slippy map (XYZ / OSM) tile indices for the spherical Mercator projection used by Mapbox and most web maps.
/// </summary>
public static class WebMercatorTileMath
{
    /// <summary>
    /// Converts WGS84 lon/lat to tile coordinates at <paramref name="zoom"/> (0–22).
    /// </summary>
    public static (int X, int Y) LonLatToTileXY(double longitudeDegrees, double latitudeDegrees, int zoom)
    {
        if (zoom is < 0 or > 22)
            throw new ArgumentOutOfRangeException(nameof(zoom), zoom, "Zoom must be in [0,22].");

        latitudeDegrees = Math.Clamp(latitudeDegrees, -85.05112878, 85.05112878);
        longitudeDegrees = Math.Clamp(longitudeDegrees, -180.0, 180.0);

        var n = Math.Pow(2, zoom);
        var x = (int)Math.Floor((longitudeDegrees + 180.0) / 360.0 * n);

        var latRad = latitudeDegrees * (Math.PI / 180.0);
        var y = (int)Math.Floor(
            (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI)
            / 2.0 * n);

        var max = (int)n - 1;
        x = Math.Clamp(x, 0, max);
        y = Math.Clamp(y, 0, max);
        return (x, y);
    }

    /// <summary>
    /// North-west corner (upper-left) lon/lat of the tile in WGS84 degrees.
    /// </summary>
    public static (double LongitudeDegrees, double LatitudeDegrees) TileXYToNwLonLat(int tileX, int tileY, int zoom)
    {
        if (zoom is < 0 or > 22)
            throw new ArgumentOutOfRangeException(nameof(zoom));

        var n = Math.Pow(2, zoom);
        var lon = tileX / n * 360.0 - 180.0;
        var latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n)));
        var lat = latRad * (180.0 / Math.PI);
        return (lon, lat);
    }
}
