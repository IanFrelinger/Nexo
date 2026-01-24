namespace Nexo.GeoTerrain;

/// <summary>
/// Minimal Web Mercator tile math utilities.
/// </summary>
public static class WebMercatorTileMath
{
    public const int DefaultTileSizePixels = 256;

    public static GeoBounds TileBounds(int z, int x, int y)
    {
        var n = Math.Pow(2.0, z);
        var lonMin = x / n * 360.0 - 180.0;
        var lonMax = (x + 1) / n * 360.0 - 180.0;

        var latMin = RadToDeg(Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * (y + 1) / n))));
        var latMax = RadToDeg(Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * y / n))));

        return new GeoBounds
        {
            MinLatitude = new Latitude(latMin),
            MinLongitude = new Longitude(lonMin),
            MaxLatitude = new Latitude(latMax),
            MaxLongitude = new Longitude(lonMax)
        };
    }

    public static GeoPoint TileCenter(int z, int x, int y)
    {
        var b = TileBounds(z, x, y);
        return new GeoPoint
        {
            Latitude = new Latitude((b.MinLatitude.Degrees + b.MaxLatitude.Degrees) * 0.5),
            Longitude = new Longitude((b.MinLongitude.Degrees + b.MaxLongitude.Degrees) * 0.5)
        };
    }

    public static double MetersPerPixelAtLatitude(int z, double latitudeDeg, int tileSizePixels = 256)
    {
        // Earth circumference in meters at equator for Web Mercator.
        const double earthCircumference = 40075016.686;
        var latRad = latitudeDeg * (Math.PI / 180.0);
        var scale = Math.Cos(latRad);
        var denom = Math.Pow(2.0, z) * tileSizePixels;
        return (earthCircumference * scale) / denom;
    }

    public static (int X, int Y) LonLatToTileXY(int z, double longitudeDeg, double latitudeDeg)
    {
        var n = Math.Pow(2.0, z);
        var x = (longitudeDeg + 180.0) / 360.0 * n;

        var latRad = latitudeDeg * (Math.PI / 180.0);
        var y = (1.0 - Math.Log(Math.Tan(latRad) + (1.0 / Math.Cos(latRad))) / Math.PI) / 2.0 * n;

        return ((int)Math.Floor(x), (int)Math.Floor(y));
    }

    public static (double Px, double Py) LonLatToGlobalPixelXY(int z, double longitudeDeg, double latitudeDeg, int tileSizePixels = DefaultTileSizePixels)
    {
        var n = Math.Pow(2.0, z);
        var x = (longitudeDeg + 180.0) / 360.0 * n;

        var latRad = latitudeDeg * (Math.PI / 180.0);
        var y = (1.0 - Math.Log(Math.Tan(latRad) + (1.0 / Math.Cos(latRad))) / Math.PI) / 2.0 * n;

        return (x * tileSizePixels, y * tileSizePixels);
    }

    public static IReadOnlyList<(int X, int Y)> TilesCovering(GeoBounds bounds, int z)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(bounds);
#else
        if (bounds is null) throw new ArgumentNullException(nameof(bounds));
#endif
        bounds.Validate();

        var (minX, maxX, minY, maxY) = TileRange(bounds, z);
        var tiles = new List<(int X, int Y)>();
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                tiles.Add((x, y));
            }
        }
        return tiles;
    }

    public static (int MinX, int MaxX, int MinY, int MaxY) TileRange(GeoBounds bounds, int z)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(bounds);
#else
        if (bounds is null) throw new ArgumentNullException(nameof(bounds));
#endif
        bounds.Validate();

        // Note: WebMercator Y increases southward.
        var (x0, y0) = LonLatToTileXY(z, bounds.MinLongitude.Degrees, bounds.MaxLatitude.Degrees); // NW corner
        var (x1, y1) = LonLatToTileXY(z, bounds.MaxLongitude.Degrees, bounds.MinLatitude.Degrees); // SE corner

        var minX = Math.Min(x0, x1);
        var maxX = Math.Max(x0, x1);
        var minY = Math.Min(y0, y1);
        var maxY = Math.Max(y0, y1);

        return (minX, maxX, minY, maxY);
    }

    private static double RadToDeg(double rad) => rad * (180.0 / Math.PI);
}

