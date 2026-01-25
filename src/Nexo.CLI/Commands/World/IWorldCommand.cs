namespace Nexo.CLI.Commands.World;

/// <summary>
/// Interface for world bundle generation commands.
/// </summary>
public interface IWorldCommand
{
    Task<int> BuildAsync(
        string bounds,
        DirectoryInfo outDir,
        string terrainElevationProvider,
        string? terrainLocalRoot,
        string? terrainSrtmBaseUrl,
        bool terrainPersistDownloads,
        bool terrainEnableCache,
        string vectorProvider,
        string? osmPbfPath,
        string? mapboxAccessToken,
        string? mapboxTileset,
        int? mapboxZoom,
        int terrainChunkSamples,
        string? terrainLodFactors,
        string? lodTriBudgets,
        int instancesChunkSamples,
        bool enableTerrainImagery,
        string? terrainImageryTileset,
        string? terrainImageryFormat,
        int? terrainImageryZoom,
        bool waterFlattenToTerrain,
        string meshFormat,
        string projection,
        bool generateVectorTextures,
        bool airGapped,
        bool json,
        bool verbose,
        CancellationToken ct);

    Task<int> ValidateAsync(
        DirectoryInfo bundleDir,
        bool json,
        bool verbose,
        CancellationToken ct);
}
