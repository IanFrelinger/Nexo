using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nexo.Adapters.GeoTerrain.Providers;

public sealed record RasterMosaic
{
    public required int Zoom { get; init; }
    public required int MinTileX { get; init; }
    public required int MinTileY { get; init; }
    public required int TileSizePixels { get; init; }
    public required int TilesWide { get; init; }
    public required int TilesHigh { get; init; }
    public required byte[] PngBytes { get; init; }
}

/// <summary>
/// Downloads raster tiles covering bounds and stitches them into a single PNG mosaic.
/// Intended for small-area demos (Unity-first).
/// </summary>
public sealed class MapboxRasterMosaicBuilder
{
    private readonly MapboxRasterTileDownloader _downloader;
    private readonly ILogger<MapboxRasterMosaicBuilder> _logger;

    public MapboxRasterMosaicBuilder(MapboxRasterTileDownloader downloader, ILogger<MapboxRasterMosaicBuilder>? logger = null)
    {
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MapboxRasterMosaicBuilder>.Instance;
    }

    public async Task<RasterMosaic> BuildAsync(GeoBounds bounds, int zoom, string tilesetId, string format, CancellationToken ct)
    {
        bounds.Validate();
        if (zoom < 0) throw new ArgumentOutOfRangeException(nameof(zoom));
        if (string.IsNullOrWhiteSpace(tilesetId)) throw new ArgumentException("tilesetId is required.", nameof(tilesetId));
        if (string.IsNullOrWhiteSpace(format)) throw new ArgumentException("format is required.", nameof(format));

        var (minX, maxX, minY, maxY) = WebMercatorTileMath.TileRange(bounds, zoom);
        var tilesWide = (maxX - minX) + 1;
        var tilesHigh = (maxY - minY) + 1;

        // Download first tile to learn tile size.
        var firstBytes = await _downloader.DownloadAsync(tilesetId, zoom, minX, minY, format, ct);
        using var first = Image.Load<Rgba32>(firstBytes);
        var tileSize = first.Width;
        if (first.Width != first.Height)
            throw new InvalidOperationException("Expected square raster tiles.");

        using var mosaic = new Image<Rgba32>(tileSize * tilesWide, tileSize * tilesHigh);
        first.Mutate(ctx => { });
        mosaic.Mutate(ctx => ctx.DrawImage(first, new SixLabors.ImageSharp.Point(0, 0), 1.0f));

        // Download remaining tiles row-major with partial failure handling.
        var failedTiles = new List<string>();
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                if (x == minX && y == minY) continue;
                try
                {
                    var bytes = await _downloader.DownloadAsync(tilesetId, zoom, x, y, format, ct);
                    using var img = Image.Load<Rgba32>(bytes);
                    if (img.Width != tileSize || img.Height != tileSize)
                    {
                        _logger.LogWarning("Inconsistent raster tile size for z={Z} x={X} y={Y}, skipping", zoom, x, y);
                        failedTiles.Add($"z={zoom} x={x} y={y} (size mismatch)");
                        continue;
                    }

                    var ox = (x - minX) * tileSize;
                    var oy = (y - minY) * tileSize;
                    mosaic.Mutate(ctx => ctx.DrawImage(img, new SixLabors.ImageSharp.Point(ox, oy), 1.0f));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download raster tile z={Z} x={X} y={Y}, continuing with partial mosaic", zoom, x, y);
                    failedTiles.Add($"z={zoom} x={x} y={y} ({ex.Message})");
                    // Fill with black/transparent for missing tile
                    var ox = (x - minX) * tileSize;
                    var oy = (y - minY) * tileSize;
                    for (var ty = 0; ty < tileSize; ty++)
                    {
                        for (var tx = 0; tx < tileSize; tx++)
                        {
                            if (ox + tx < mosaic.Width && oy + ty < mosaic.Height)
                            {
                                mosaic[ox + tx, oy + ty] = new Rgba32(0, 0, 0, 0);
                            }
                        }
                    }
                }
            }
        }

        if (failedTiles.Count > 0)
        {
            _logger.LogWarning("Mosaic built with {FailedCount} failed tiles out of {TotalTiles}", failedTiles.Count, (tilesWide * tilesHigh));
        }

        using var ms = new MemoryStream();
        await mosaic.SaveAsPngAsync(ms, ct);
        var png = ms.ToArray();

        _logger.LogInformation("Built raster mosaic tiles={W}x{H} px={PxW}x{PxH} z={Z}", tilesWide, tilesHigh, mosaic.Width, mosaic.Height, zoom);

        return new RasterMosaic
        {
            Zoom = zoom,
            MinTileX = minX,
            MinTileY = minY,
            TileSizePixels = tileSize,
            TilesWide = tilesWide,
            TilesHigh = tilesHigh,
            PngBytes = png
        };
    }
}

