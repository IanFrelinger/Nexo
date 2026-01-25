namespace Nexo.CLI.Commands.GeoTerrain;

/// <summary>
/// Interface for geospatial terrain generation commands.
/// </summary>
public interface IGeoTerrainCommand
{
    Task<int> BoundsToObjAsync(
        string bounds,
        FileInfo output,
        string provider,
        string? localRoot,
        string? srtmBaseUrl,
        bool persistDownloads,
        bool enableCache,
        bool airGapped,
        bool forceAgenticFail,
        bool validateIntegrity,
        bool meshQualityReport,
        string? cacheRoot,
        bool persistCache,
        bool json,
        bool verbose,
        CancellationToken ct);

    Task<int> TileToObjAsync(
        string tile,
        FileInfo output,
        string provider,
        string? localRoot,
        string? srtmBaseUrl,
        bool persistDownloads,
        bool enableCache,
        bool airGapped,
        bool forceAgenticFail,
        string? cacheRoot,
        bool persistCache,
        bool json,
        bool verbose,
        CancellationToken ct);
}
