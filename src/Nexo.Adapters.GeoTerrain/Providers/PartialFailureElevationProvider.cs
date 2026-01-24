using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.GeoWorld.Results;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Wraps an IElevationProvider to support partial failure handling.
/// Returns partial results when some tiles succeed and others fail.
/// </summary>
public sealed class PartialFailureElevationProvider : IElevationProvider
{
    private readonly IElevationProvider _inner;
    private readonly ILogger<PartialFailureElevationProvider>? _logger;

    public PartialFailureElevationProvider(
        IElevationProvider inner,
        ILogger<PartialFailureElevationProvider>? logger = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PartialFailureElevationProvider>.Instance;
    }

    /// <summary>
    /// Gets a single tile (standard interface).
    /// </summary>
    public Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default)
    {
        return _inner.GetSrtmTileAsync(tileId, cancellationToken);
    }

    /// <summary>
    /// Gets multiple tiles with partial failure support.
    /// </summary>
    public async Task<PartialResult<ElevationTile>> GetTilesAsync(
        IReadOnlyList<SrtmTileId> tileIds,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ElevationTile>();
        var failures = new List<string>();

        foreach (var tileId in tileIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var tile = await _inner.GetSrtmTileAsync(tileId, cancellationToken);
                results.Add(tile);
            }
            catch (Exception ex)
            {
                var error = $"Failed to get tile {tileId}: {ex.Message}";
                failures.Add(error);
                _logger?.LogWarning(ex, "Failed to get elevation tile {TileId}", tileId);
            }
        }

        if (failures.Count == 0)
            return PartialResult.Successful(results);

        if (results.Count == 0)
            return PartialResult.Failed<ElevationTile>(failures);

        return PartialResult.PartialSuccess(results, failures);
    }
}
