using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.Adapters.GeoTerrain.Providers;

/// <summary>
/// Offline placeholder provider. Returns a tiny, synthetic 2x2 tile (all zeros).
/// </summary>
public sealed class EchoElevationProvider : IElevationProvider
{
    public Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default)
    {
        // 2x2 samples -> 8 bytes.
        var bytes = new byte[]
        {
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00
        };

        var summary = SrtmHgtParser.Analyze(bytes);
        var tile = new ElevationTile
        {
            TileId = tileId,
            HgtBytes = bytes,
            Size = summary.Size,
            MinMeters = summary.MinMeters,
            MaxMeters = summary.MaxMeters,
            NoDataSamples = summary.NoDataSamples,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "echo"
            }
        };

        return Task.FromResult(tile);
    }
}

