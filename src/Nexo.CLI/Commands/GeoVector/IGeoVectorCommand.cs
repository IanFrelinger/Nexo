namespace Nexo.CLI.Commands.GeoVector;

/// <summary>
/// Interface for geospatial vector feature extraction commands.
/// </summary>
public interface IGeoVectorCommand
{
    Task<int> BuildingsToObjAsync(
        string bounds,
        FileInfo output,
        string provider,
        string? mapboxAccessToken,
        string? mapboxTileset,
        int? mapboxZoom,
        string? osmPbfPath,
        bool generateTexCoords,
        float uvMetersPerRepeat,
        bool alignToTerrain,
        string terrainProvider,
        string? terrainLocalRoot,
        string? terrainSrtmBaseUrl,
        bool terrainPersistDownloads,
        bool terrainEnableCache,
        bool terrainTreatNoDataAsZero,
        bool airGapped,
        bool forceAgenticFail,
        bool json,
        bool verbose,
        CancellationToken ct);

    Task<int> RoadsToObjAsync(
        string bounds,
        FileInfo output,
        string provider,
        string? mapboxAccessToken,
        string? mapboxTileset,
        int? mapboxZoom,
        string? osmPbfPath,
        float widthMeters,
        bool generateTexCoords,
        float uvMetersPerRepeat,
        bool conformToTerrain,
        string terrainProvider,
        string? terrainLocalRoot,
        string? terrainSrtmBaseUrl,
        bool terrainPersistDownloads,
        bool terrainEnableCache,
        bool terrainTreatNoDataAsZero,
        bool airGapped,
        bool forceAgenticFail,
        bool json,
        bool verbose,
        CancellationToken ct);

    Task<int> WaterToObjAsync(
        string bounds,
        FileInfo output,
        string provider,
        string? mapboxAccessToken,
        string? mapboxTileset,
        int? mapboxZoom,
        string? osmPbfPath,
        bool generateTexCoords,
        float uvMetersPerRepeat,
        bool conformToTerrain,
        float surfaceOffsetMeters,
        string terrainProvider,
        string? terrainLocalRoot,
        string? terrainSrtmBaseUrl,
        bool terrainPersistDownloads,
        bool terrainEnableCache,
        bool terrainTreatNoDataAsZero,
        bool airGapped,
        bool forceAgenticFail,
        bool json,
        bool verbose,
        CancellationToken ct);
}
