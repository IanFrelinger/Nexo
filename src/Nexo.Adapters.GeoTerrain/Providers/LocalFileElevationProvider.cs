using Microsoft.Extensions.Logging;
using Nexo.Adapters.GeoTerrain.Parsing;
using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Parsing;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Loads elevation data from local files, auto-detecting format by extension.
/// Supports: .hgt (SRTM), .asc/.txt (ASCII Grid), .tif/.tiff (GeoTIFF).
/// </summary>
public sealed class LocalFileElevationProvider : IElevationProvider
{
    private readonly string _root;
    private readonly ILogger<LocalFileElevationProvider>? _logger;

    public string RootPath => _root;

    public LocalFileElevationProvider(string? rootPath, ILogger<LocalFileElevationProvider>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("LocalRootPath must be provided for Local elevation provider.", nameof(rootPath));
        _root = rootPath!;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<LocalFileElevationProvider>.Instance;
    }

    public async Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default)
    {
        // Try multiple formats in order of preference
        var candidates = new[]
        {
            tileId + ".hgt",
            tileId + ".asc",
            tileId + ".txt",
            tileId + ".tif",
            tileId + ".tiff"
        };

        foreach (var fileName in candidates)
        {
            var full = Path.Combine(_root, fileName);
            if (!File.Exists(full)) continue;

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".hgt" => await LoadHgtAsync(full, tileId, cancellationToken),
                ".asc" or ".txt" => await LoadAsciiGridAsync(full, tileId, cancellationToken),
                ".tif" or ".tiff" => await LoadGeoTiffAsync(full, tileId, cancellationToken),
                _ => throw new NotSupportedException($"Unsupported elevation format: {ext}")
            };
        }

        throw new FileNotFoundException($"No elevation file found for tile {tileId} in {_root}. Tried: {string.Join(", ", candidates)}");
    }

    private async Task<ElevationTile> LoadHgtAsync(string fullPath, SrtmTileId tileId, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(fullPath, ct);
        var summary = SrtmHgtParser.Analyze(bytes);

        return new ElevationTile
        {
            TileId = tileId,
            HgtBytes = bytes,
            Size = summary.Size,
            MinMeters = summary.MinMeters,
            MaxMeters = summary.MaxMeters,
            NoDataSamples = summary.NoDataSamples,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "local",
                ["format"] = "hgt",
                ["path"] = fullPath
            }
        };
    }

    private async Task<ElevationTile> LoadAsciiGridAsync(string fullPath, SrtmTileId tileId, CancellationToken ct)
    {
        var text = await File.ReadAllTextAsync(fullPath, ct);
        var grid = AsciiGridParser.Parse(text, boundsOverride: tileId.ToBounds());

        return ConvertGridToTile(grid, tileId, fullPath, "ascii-grid");
    }

    private async Task<ElevationTile> LoadGeoTiffAsync(string fullPath, SrtmTileId tileId, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(fullPath, ct);
        var grid = GeoTiffParser.Parse(bytes, bounds: tileId.ToBounds());

        return ConvertGridToTile(grid, tileId, fullPath, "geotiff");
    }

    private static ElevationTile ConvertGridToTile(ElevationGrid grid, SrtmTileId tileId, string path, string format)
    {
        var heights = grid.CloneHeightsMeters();
        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;
        var nodata = 0;

        for (var i = 0; i < heights.Length; i++)
        {
            var v = heights[i];
            if (float.IsNaN(v))
            {
                nodata++;
                continue;
            }
            if (v < min) min = v;
            if (v > max) max = v;
        }

        if (float.IsPositiveInfinity(min)) min = 0f;
        if (float.IsNegativeInfinity(max)) max = 0f;

        // Convert to HGT format (signed 16-bit, big-endian)
        var hgtBytes = new byte[heights.Length * 2];
        for (var i = 0; i < heights.Length; i++)
        {
            var v = float.IsNaN(heights[i]) ? SrtmHgtParser.NoDataSentinel : (short)Math.Round(heights[i]);
            hgtBytes[i * 2] = (byte)((v >> 8) & 0xFF);
            hgtBytes[i * 2 + 1] = (byte)(v & 0xFF);
        }

        return new ElevationTile
        {
            TileId = tileId,
            HgtBytes = hgtBytes,
            Size = grid.Width,
            MinMeters = (short)Math.Round(min),
            MaxMeters = (short)Math.Round(max),
            NoDataSamples = nodata,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "local",
                ["format"] = format,
                ["path"] = path,
                ["width"] = grid.Width,
                ["height"] = grid.Height
            }
        };
    }
}

