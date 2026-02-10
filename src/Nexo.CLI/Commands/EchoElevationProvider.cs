using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoTerrain.Ports;

namespace Nexo.CLI.Commands;

/// <summary>
/// Stub elevation provider. Returns a minimal synthetic tile (no real terrain data).
/// Used when GeoTerrain adapters are not available.
/// </summary>
public sealed class EchoElevationProvider : IElevationProvider
{
    public Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default)
    {
        var bytes = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x14 };
        var tile = new ElevationTile
        {
            TileId = tileId,
            HgtBytes = bytes,
            Size = 2,
            MinMeters = 0,
            MaxMeters = 20,
            NoDataSamples = 0,
            Metadata = new Dictionary<string, object> { ["provider"] = "echo" }
        };
        return Task.FromResult(tile);
    }
}
