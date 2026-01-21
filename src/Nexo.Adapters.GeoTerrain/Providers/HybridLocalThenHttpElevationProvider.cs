using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Provider that tries local disk first, then downloads via HTTP if missing.
/// Optionally persists downloaded tiles back to the local directory for future air-gapped runs.
/// </summary>
public sealed class HybridLocalThenHttpElevationProvider : IElevationProvider
{
    private readonly LocalFileElevationProvider _local;
    private readonly SrtmHttpElevationProvider _http;
    private readonly bool _persistToLocal;
    private readonly ILogger<HybridLocalThenHttpElevationProvider> _logger;

    public HybridLocalThenHttpElevationProvider(
        LocalFileElevationProvider local,
        SrtmHttpElevationProvider http,
        bool persistToLocal,
        ILogger<HybridLocalThenHttpElevationProvider> logger)
    {
        _local = local ?? throw new ArgumentNullException(nameof(local));
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _persistToLocal = persistToLocal;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var localTile = await _local.GetSrtmTileAsync(tileId, cancellationToken);
            if (localTile.Metadata is Dictionary<string, object> md)
            {
                md["provider"] = "hybrid:local";
            }
            return localTile;
        }
        catch (FileNotFoundException)
        {
            _logger.LogInformation("Local tile missing; downloading {Tile}", tileId.ToString());
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogInformation("Local directory missing; downloading {Tile}", tileId.ToString());
        }

        var downloaded = await _http.GetSrtmTileAsync(tileId, cancellationToken);

        if (_persistToLocal)
        {
            try
            {
                // Persist to local root by invoking the local provider's convention (tileId + ".hgt").
                // We assume LocalFileElevationProvider was configured with a valid root path.
                var root = _local.RootPath;
                if (!string.IsNullOrWhiteSpace(root))
                {
                    Directory.CreateDirectory(root);
                    var full = Path.Combine(root, tileId + ".hgt");
                    if (!File.Exists(full))
                    {
                        await File.WriteAllBytesAsync(full, downloaded.HgtBytes, cancellationToken);
                        _logger.LogInformation("Persisted downloaded tile to {Path}", full);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to persist downloaded tile locally (continuing).");
            }
        }

        // Tag metadata.
        var meta = downloaded.Metadata is null ? new Dictionary<string, object>() : new Dictionary<string, object>(downloaded.Metadata);
        meta["provider"] = "hybrid:http";

        return downloaded with { Metadata = meta };
    }
}

