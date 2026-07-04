namespace Nexo.Commercial.GameDomain.Mapping;
/// <summary>
/// HTTPS URLs for common terrain / elevation raster tiles (host-side orchestration).
/// </summary>
public static class TerrainTileUrlBuilder
{
    /// <summary>
    /// Mapbox Terrain-RGB raster tile (<c>mapbox.terrain-rgb</c>) as raw PNG (<c>.pngraw</c>).
    /// </summary>
    /// <param name="z">Zoom level 0–22.</param>
    /// <param name="x">Tile X index.</param>
    /// <param name="y">Tile Y index.</param>
    /// <param name="accessToken">Mapbox access token.</param>
    public static string MapboxTerrainRgbTileUrl(int z, int x, int y, string accessToken) =>
        MapboxTerrainRgbTileUrl("mapbox.terrain-rgb", z, x, y, accessToken);

    /// <summary>
    /// Mapbox raster tile URL for a terrain tileset path (Terrain-RGB uses <c>.pngraw</c>).
    /// </summary>
    /// <param name="tilesetPath">e.g. <c>mapbox.terrain-rgb</c>.</param>
    /// <param name="z">Zoom level 0–22.</param>
    /// <param name="x">Tile X index.</param>
    /// <param name="y">Tile Y index.</param>
    /// <param name="accessToken">Mapbox access token.</param>
    public static string MapboxTerrainRgbTileUrl(string tilesetPath, int z, int x, int y, string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tilesetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (z is < 0 or > 22)
            throw new ArgumentOutOfRangeException(nameof(z));

        var trimmed = tilesetPath.Trim().Trim('/');
        var token = Uri.EscapeDataString(accessToken);
        return $"https://api.mapbox.com/v4/{trimmed}/{z}/{x}/{y}.pngraw?access_token={token}";
    }
}
