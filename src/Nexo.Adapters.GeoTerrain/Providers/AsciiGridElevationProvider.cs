using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Parsing;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Loads elevation data from ASCII Grid files (local disk, air-gapped friendly).
/// Files are expected to be in ESRI ASCII Grid format (.asc, .txt).
/// </summary>
public sealed class AsciiGridElevationProvider : IElevationProvider
{
    private readonly string _filePath;
    private readonly ILogger<AsciiGridElevationProvider>? _logger;

    public AsciiGridElevationProvider(string filePath, ILogger<AsciiGridElevationProvider>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required.", nameof(filePath));
        _filePath = filePath;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AsciiGridElevationProvider>.Instance;
    }

    public Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default)
    {
        // ASCII Grid files are typically single-file datasets, not tile-based.
        // For now, we'll read the file and create a synthetic tile ID.
        // In the future, we could support multi-file ASCII Grid datasets.
        
        if (!File.Exists(_filePath))
            throw new FileNotFoundException($"ASCII Grid file not found: {_filePath}");

        var text = File.ReadAllText(_filePath);
        var grid = AsciiGridParser.Parse(text);

        // Convert ElevationGrid to ElevationTile format
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

        // Create a synthetic tile ID from the file path
        var syntheticTileId = new SrtmTileId(
            (int)Math.Floor(grid.Bounds.MinLatitude.Degrees),
            (int)Math.Floor(grid.Bounds.MinLongitude.Degrees));

        // Convert heights to HGT format (signed 16-bit, big-endian)
        var hgtBytes = new byte[heights.Length * 2];
        for (var i = 0; i < heights.Length; i++)
        {
            var v = float.IsNaN(heights[i]) ? SrtmHgtParser.NoDataSentinel : (short)Math.Round(heights[i]);
            hgtBytes[i * 2] = (byte)((v >> 8) & 0xFF);
            hgtBytes[i * 2 + 1] = (byte)(v & 0xFF);
        }

        _logger?.LogInformation("Loaded ASCII Grid from {Path}: {W}x{H}, min={Min}m max={Max}m", 
            _filePath, grid.Width, grid.Height, min, max);

        return Task.FromResult(new ElevationTile
        {
            TileId = syntheticTileId,
            HgtBytes = hgtBytes,
            Size = grid.Width, // Assuming square grid
            MinMeters = (short)Math.Round(min),
            MaxMeters = (short)Math.Round(max),
            NoDataSamples = nodata,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "ascii-grid",
                ["path"] = _filePath,
                ["width"] = grid.Width,
                ["height"] = grid.Height
            }
        });
    }
}
