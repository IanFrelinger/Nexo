namespace Ashlar.Tests.Infrastructure.External;

/// <summary>
/// Builds Mapbox Raster / Vector Tile API v4 URLs and validates Web Mercator tile indices.
/// Used by integration tests; keep token out of logs and source control (pass via env only).
/// </summary>
public static class MapboxTileUrls
{
    public const string DefaultRasterTilesetId = "mapbox.satellite";
    public const string DefaultVectorTilesetId = "mapbox.mapbox-streets-v8";
    public const string RasterHost = "https://api.mapbox.com";

    /// <summary>Throws if tile indices are invalid for standard XYZ Web Mercator tiling at zoom <paramref name="z"/>.</summary>
    public static void ValidateWebMercatorTile(int z, int x, int y)
    {
        if (z < 0 || z > 30)
            throw new ArgumentOutOfRangeException(nameof(z), z, "Zoom must be in [0, 30].");

        var extent = 1 << z;
        if (x < 0 || x >= extent)
            throw new ArgumentOutOfRangeException(nameof(x), x, $"Tile X must be in [0, {extent - 1}] for z={z}.");
        if (y < 0 || y >= extent)
            throw new ArgumentOutOfRangeException(nameof(y), y, $"Tile Y must be in [0, {extent - 1}] for z={z}.");
    }

    /// <summary>
    /// Raster tile URL, e.g. <c>.../v4/mapbox.satellite/0/0/0.jpg?access_token=...</c>.
    /// </summary>
    /// <param name="rasterSuffix">File suffix including dot, e.g. <c>.jpg</c> or <c>@2x.jpg</c>.</param>
    public static string BuildRasterTileUrl(
        string tilesetId,
        int z,
        int x,
        int y,
        string accessToken,
        string rasterSuffix = ".jpg")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tilesetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ValidateWebMercatorTile(z, x, y);

        var suffix = string.IsNullOrWhiteSpace(rasterSuffix) ? ".jpg" : rasterSuffix.Trim();
        if (!suffix.StartsWith("@", StringComparison.Ordinal) && !suffix.StartsWith(".", StringComparison.Ordinal))
            suffix = "." + suffix.TrimStart('.');

        return $"{RasterHost}/v4/{Uri.EscapeDataString(tilesetId.Trim())}/{z}/{x}/{y}{suffix}?access_token={Uri.EscapeDataString(accessToken.Trim())}";
    }

    /// <summary>
    /// Vector tile URL (MVT), e.g. <c>.../v4/mapbox.mapbox-streets-v8/0/0/0.vector.pbf?access_token=...</c>.
    /// </summary>
    public static string BuildVectorTileUrl(string tilesetId, int z, int x, int y, string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tilesetId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ValidateWebMercatorTile(z, x, y);

        return $"{RasterHost}/v4/{Uri.EscapeDataString(tilesetId.Trim())}/{z}/{x}/{y}.vector.pbf?access_token={Uri.EscapeDataString(accessToken.Trim())}";
    }

    /// <summary>True if <paramref name="bytes"/> looks like JPEG (SOI marker).</summary>
    public static bool IsJpegImage(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8;

    /// <summary>True if payload is gzip-compressed (common for vector tiles).</summary>
    public static bool IsGzipPayload(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 2 && bytes[0] == 0x1F && bytes[1] == 0x8B;

    /// <summary>Heuristic: Mapbox vector tile responses often use these content types.</summary>
    public static bool IsLikelyVectorTileContentType(string? mediaType)
    {
        if (string.IsNullOrEmpty(mediaType))
            return true;

        return mediaType.Contains("protobuf", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Contains("octet-stream", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Contains("mapbox-vector-tile", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Contains("gzip", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Contains("application/x-protobuf", StringComparison.OrdinalIgnoreCase);
    }
}
