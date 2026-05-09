namespace Nexo.Tests.Infrastructure.External;

/// <summary>
/// Web Mercator (EPSG:3857) tile index math for standard XYZ tiling used by Mapbox raster/vector APIs.
/// </summary>
public static class MapboxWebMercatorTileMath
{
    /// <summary>Validates zoom only; tile count at zoom z is a power of two.</summary>
    public static void ValidateZoom(int z)
    {
        if (z < 0 || z > 30)
            throw new ArgumentOutOfRangeException(nameof(z), z, "Zoom must be in [0, 30].");
    }

    /// <summary>
    /// Converts WGS84 latitude/longitude in degrees to tile X/Y at zoom <paramref name="z"/> (XYZ scheme, y down).
    /// Results are clamped to valid tile bounds for the zoom level.
    /// </summary>
    public static (int X, int Y) TileIndexFromLatLon(double latitudeDegrees, double longitudeDegrees, int z)
    {
        ValidateZoom(z);

        var n = 1 << z;
        var x = (int)Math.Floor((longitudeDegrees + 180.0) / 360.0 * n);
        var latRad = latitudeDegrees * (Math.PI / 180.0);
        var y = (int)Math.Floor(
            (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);

        x = Math.Clamp(x, 0, n - 1);
        y = Math.Clamp(y, 0, n - 1);

        MapboxTileUrls.ValidateWebMercatorTile(z, x, y);
        return (x, y);
    }
}
