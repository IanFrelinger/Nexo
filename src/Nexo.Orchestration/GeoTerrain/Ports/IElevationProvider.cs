using Nexo.GeoTerrain;

namespace Nexo.Orchestration.GeoTerrain.Ports;

/// <summary>
/// Port for obtaining elevation tiles (air-gapped friendly via Local providers, online via HTTP providers).
/// Mirrors the Assets port pattern in <c>Nexo.Orchestration/Assets/Ports</c>.
/// </summary>
public interface IElevationProvider
{
    Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default);
}

