using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using Nexo.CLI.Commands.GeoVector;

namespace Nexo.API.Services;

/// <summary>
/// Implementation of vector feature extraction service.
/// </summary>
public class GeoVectorService : BaseGeospatialService<IGeoVectorCommand>, IGeoVectorService
{
    public GeoVectorService(
        IGeoVectorCommand command,
        ILogger<GeoVectorService> logger,
        IJobRepository jobRepository,
        WebhookService? webhookService = null)
        : base(command, logger, jobRepository, webhookService, "geovector")
    {
    }

    public async Task<string> ExtractFeaturesAsync(VectorExtractionRequest request)
    {
        var (jobId, outputPath) = await CreateJobAsync(request.Format, request.WebhookUrl);
        var outputFile = new FileInfo(outputPath);

        var bounds = ParseBounds(request.Bounds);
        var provider = request.VectorProvider ?? "hybrid";
        var mapboxToken = request.MapboxToken ?? Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");
        var osmPbfPath = request.OsmPbfPath ?? request.VectorFilePath;
        var featureKind = request.FeatureKind?.Trim().ToLowerInvariant() ?? "building";

        StartJobProcessing(
            jobId,
            request.WebhookUrl,
            async (job) =>
            {
                job = job with { Progress = 20 };
                await _jobRepository.UpdateJobAsync(job);

                return featureKind switch
                {
                    "building" => await _command.BuildingsToObjAsync(
                        bounds: bounds,
                        output: outputFile,
                        provider: provider,
                        mapboxAccessToken: mapboxToken,
                        mapboxTileset: null,
                        mapboxZoom: null,
                        osmPbfPath: osmPbfPath,
                        generateTexCoords: false,
                        uvMetersPerRepeat: 10.0f,
                        alignToTerrain: false,
                        terrainProvider: "srtm",
                        terrainLocalRoot: null,
                        terrainSrtmBaseUrl: null,
                        terrainPersistDownloads: true,
                        terrainEnableCache: true,
                        terrainTreatNoDataAsZero: false,
                        airGapped: false,
                        forceAgenticFail: false,
                        cacheRoot: request.CacheRoot,
                        persistCache: request.PersistCache ?? true,
                        terrainCacheRoot: request.CacheRoot,
                        terrainPersistCache: request.PersistCache ?? true,
                        json: false,
                        verbose: true,
                        ct: CancellationToken.None),
                    "road" or "roads" => await _command.RoadsToObjAsync(
                        bounds: bounds,
                        output: outputFile,
                        provider: provider,
                        mapboxAccessToken: mapboxToken,
                        mapboxTileset: null,
                        mapboxZoom: null,
                        osmPbfPath: osmPbfPath,
                        widthMeters: 4.0f,
                        generateTexCoords: false,
                        uvMetersPerRepeat: 10.0f,
                        conformToTerrain: false,
                        terrainProvider: "srtm",
                        terrainLocalRoot: null,
                        terrainSrtmBaseUrl: null,
                        terrainPersistDownloads: true,
                        terrainEnableCache: true,
                        terrainTreatNoDataAsZero: false,
                        airGapped: false,
                        forceAgenticFail: false,
                        cacheRoot: request.CacheRoot,
                        persistCache: request.PersistCache ?? true,
                        terrainCacheRoot: request.CacheRoot,
                        terrainPersistCache: request.PersistCache ?? true,
                        json: false,
                        verbose: true,
                        ct: CancellationToken.None),
                    "water" => await _command.WaterToObjAsync(
                        bounds: bounds,
                        output: outputFile,
                        provider: provider,
                        mapboxAccessToken: mapboxToken,
                        mapboxTileset: null,
                        mapboxZoom: null,
                        osmPbfPath: osmPbfPath,
                        generateTexCoords: false,
                        uvMetersPerRepeat: 10.0f,
                        conformToTerrain: false,
                        surfaceOffsetMeters: 0.0f,
                        terrainProvider: "srtm",
                        terrainLocalRoot: null,
                        terrainSrtmBaseUrl: null,
                        terrainPersistDownloads: true,
                        terrainEnableCache: true,
                        terrainTreatNoDataAsZero: false,
                        airGapped: false,
                        forceAgenticFail: false,
                        cacheRoot: request.CacheRoot,
                        persistCache: request.PersistCache ?? true,
                        terrainCacheRoot: request.CacheRoot,
                        terrainPersistCache: request.PersistCache ?? true,
                        json: false,
                        verbose: true,
                        ct: CancellationToken.None),
                    _ => throw new NotImplementedException($"Feature kind '{request.FeatureKind}' extraction not yet implemented in API service. Supported: building, road, water")
                };
            },
            outputPath);

        return jobId;
    }
}
