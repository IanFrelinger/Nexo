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

/// <summary>
/// Represents a single SRTM tile's raw data and inferred metadata.
/// </summary>
public sealed record ElevationTile
{
    public required SrtmTileId TileId { get; init; }
    public required byte[] HgtBytes { get; init; }
    public required int Size { get; init; }
    public required short MinMeters { get; init; }
    public required short MaxMeters { get; init; }
    public required int NoDataSamples { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

