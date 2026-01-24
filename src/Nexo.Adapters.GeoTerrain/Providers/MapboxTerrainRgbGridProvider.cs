using System.Net;
using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Downloads a single Mapbox Terrain-RGB tile and decodes it into an <see cref="ElevationGrid"/>.
/// This is an optional higher-fidelity elevation source (not SRTM/HGT).
/// </summary>
public sealed class MapboxTerrainRgbGridProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<MapboxTerrainRgbGridProvider> _logger;
    private readonly string _accessToken;
    private readonly string _tilesetId;

    public MapboxTerrainRgbGridProvider(
        HttpClient httpClient,
        string accessToken,
        string tilesetId = "mapbox.terrain-rgb",
        ILogger<MapboxTerrainRgbGridProvider>? logger = null)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (string.IsNullOrWhiteSpace(accessToken)) throw new ArgumentException("accessToken is required.", nameof(accessToken));
        if (string.IsNullOrWhiteSpace(tilesetId)) throw new ArgumentException("tilesetId is required.", nameof(tilesetId));
        _accessToken = accessToken.Trim();
        _tilesetId = tilesetId.Trim();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MapboxTerrainRgbGridProvider>.Instance;
    }

    public async Task<(ElevationGrid Grid, byte[] PngBytes)> GetTileGridAsync(int z, int x, int y, CancellationToken ct)
    {
        var url = BuildUrl(_tilesetId, z, x, y, _accessToken);
        var png = await _http.GetByteArrayAsync(url, ct);

        using var image = Image.Load<Rgba32>(png);
        if (image.Width < 2 || image.Height < 2)
            throw new InvalidOperationException("Terrain-RGB tile must be at least 2x2 pixels.");

        var bounds = WebMercatorTileMath.TileBounds(z, x, y);
        var center = WebMercatorTileMath.TileCenter(z, x, y);
        var metersPerPixel = WebMercatorTileMath.MetersPerPixelAtLatitude(z, center.Latitude.Degrees, image.Width);
        var spacing = new GridSpacing(metersPerPixel, metersPerPixel);

        var heights = new float[image.Width * image.Height];
        var ii = 0;
        image.ProcessPixelRows(accessor =>
        {
            for (var row = 0; row < accessor.Height; row++)
            {
                var span = accessor.GetRowSpan(row);
                for (var col = 0; col < accessor.Width; col++)
                {
                    var px = span[col];
                    heights[ii++] = TerrainRgbDecoder.DecodeMeters(px.R, px.G, px.B);
                }
            }
        });

        _logger.LogInformation("Decoded Terrain-RGB tile z={Z} x={X} y={Y} size={W}x{H}", z, x, y, image.Width, image.Height);
        return (new ElevationGrid(image.Width, image.Height, bounds, spacing, heights), png);
    }

    private static string BuildUrl(string tilesetId, int z, int x, int y, string token)
    {
        // Mapbox tiles v4 endpoint (pngraw preserves exact values):
        // https://api.mapbox.com/v4/{tileset}/{z}/{x}/{y}.pngraw?access_token=...
        return $"https://api.mapbox.com/v4/{tilesetId}/{z}/{x}/{y}.pngraw?access_token={WebUtility.UrlEncode(token)}";
    }
}

