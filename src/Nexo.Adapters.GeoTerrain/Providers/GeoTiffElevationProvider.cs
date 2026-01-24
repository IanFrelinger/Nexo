using Microsoft.Extensions.Logging;
using Nexo.Adapters.GeoTerrain.Parsing;
using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Loads elevation data from GeoTIFF files (local disk, air-gapped friendly).
/// Files are expected to be GeoTIFF format (.tif, .tiff).
/// </summary>
public sealed class GeoTiffElevationProvider : IElevationProvider
{
    private readonly string _filePath;
    private readonly GeoBounds? _boundsOverride;
    private readonly float _scale;
    private readonly float _offset;
    private readonly float? _noDataValue;
    private readonly ILogger<GeoTiffElevationProvider>? _logger;

    public GeoTiffElevationProvider(
        string filePath,
        GeoBounds? boundsOverride = null,
        float scale = 1.0f,
        float offset = 0.0f,
        float? noDataValue = null,
        ILogger<GeoTiffElevationProvider>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("filePath is required.", nameof(filePath));
        _filePath = filePath;
        _boundsOverride = boundsOverride;
        _scale = scale;
        _offset = offset;
        _noDataValue = noDataValue;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GeoTiffElevationProvider>.Instance;
    }

    public Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            throw new FileNotFoundException($"GeoTIFF file not found: {_filePath}");

        var bytes = File.ReadAllBytes(_filePath);
        
        // Try to extract bounds from GeoTIFF tags, fall back to override or default
        var bounds = GeoTiffParser.TryExtractBounds(bytes) ?? _boundsOverride;
        if (bounds == null)
        {
            // Create a default bounds - caller should provide proper bounds
            bounds = tileId.ToBounds();
        }

        var grid = GeoTiffParser.Parse(bytes, bounds, _scale, _offset, _noDataValue);

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

        // Convert heights to HGT format (signed 16-bit, big-endian)
        var hgtBytes = new byte[heights.Length * 2];
        for (var i = 0; i < heights.Length; i++)
        {
            var v = float.IsNaN(heights[i]) ? SrtmHgtParser.NoDataSentinel : (short)Math.Round(heights[i]);
            hgtBytes[i * 2] = (byte)((v >> 8) & 0xFF);
            hgtBytes[i * 2 + 1] = (byte)(v & 0xFF);
        }

        _logger?.LogInformation("Loaded GeoTIFF from {Path}: {W}x{H}, min={Min}m max={Max}m", 
            _filePath, grid.Width, grid.Height, min, max);

        return Task.FromResult(new ElevationTile
        {
            TileId = tileId,
            HgtBytes = hgtBytes,
            Size = grid.Width, // Assuming square grid
            MinMeters = (short)Math.Round(min),
            MaxMeters = (short)Math.Round(max),
            NoDataSamples = nodata,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "geotiff",
                ["path"] = _filePath,
                ["width"] = grid.Width,
                ["height"] = grid.Height,
                ["scale"] = _scale,
                ["offset"] = _offset
            }
        });
    }
}
