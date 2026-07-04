namespace Nexo.Commercial.GameDomain.Mapping;
/// <summary>
/// Builds HTTPS URLs for common vector tile endpoints (host-side orchestration).
/// </summary>
public static class VectorTileUrlBuilder
{
    /// <summary>
    /// Mapbox Downloads API vector tile URL (<c>api.mapbox.com/v4/...</c>).
    /// </summary>
    /// <param name="tilesetPath">e.g. <c>mapbox.mapbox-streets-v8</c> or <c>mapbox.mapbox-terrain-v2</c>.</param>
    /// <param name="z">Zoom level 0–22.</param>
    /// <param name="x">Tile X index.</param>
    /// <param name="y">Tile Y index.</param>
    /// <param name="accessToken">Mapbox access token (URL-encoded).</param>
    public static string MapboxVectorTileUrl(string tilesetPath, int z, int x, int y, string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tilesetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (z is < 0 or > 22)
            throw new ArgumentOutOfRangeException(nameof(z));

        var trimmed = tilesetPath.Trim().Trim('/');
        var token = Uri.EscapeDataString(accessToken);
        return $"https://api.mapbox.com/v4/{trimmed}/{z}/{x}/{y}.vector.pbf?access_token={token}";
    }
}
