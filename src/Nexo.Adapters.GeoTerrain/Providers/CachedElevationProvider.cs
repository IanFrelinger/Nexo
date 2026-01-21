using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Simple in-memory cache decorator for <see cref="IElevationProvider"/>.
/// </summary>
public sealed class CachedElevationProvider : IElevationProvider
{
    private readonly IElevationProvider _inner;
    private readonly Dictionary<SrtmTileId, ElevationTile> _cache = new();
    private readonly object _gate = new();

    public CachedElevationProvider(IElevationProvider inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(tileId, out var cached))
            {
                return Clone(cached);
            }
        }

        var tile = await _inner.GetSrtmTileAsync(tileId, cancellationToken);

        lock (_gate)
        {
            _cache[tileId] = Clone(tile);
        }

        return Clone(tile);
    }

    private static ElevationTile Clone(ElevationTile t)
    {
        return t with
        {
            HgtBytes = (byte[])t.HgtBytes.Clone(),
            Metadata = t.Metadata is null ? null : new Dictionary<string, object>(t.Metadata)
        };
    }
}

