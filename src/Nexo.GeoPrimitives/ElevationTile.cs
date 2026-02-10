namespace Nexo.GeoTerrain;

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
