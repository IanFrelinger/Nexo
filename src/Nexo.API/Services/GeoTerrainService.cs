using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using Nexo.CLI.Commands.GeoTerrain;

namespace Nexo.API.Services;

/// <summary>
/// Implementation of terrain generation service.
/// </summary>
public class GeoTerrainService : BaseGeospatialService<IGeoTerrainCommand>, IGeoTerrainService
{
    public GeoTerrainService(
        IGeoTerrainCommand command,
        ILogger<GeoTerrainService> logger,
        IJobRepository jobRepository,
        WebhookService? webhookService = null)
        : base(command, logger, jobRepository, webhookService, "geoterrain")
    {
    }

    public async Task<string> GenerateTerrainAsync(TerrainGenerationRequest request)
    {
        var (jobId, outputPath) = await CreateJobAsync(request.Format, request.WebhookUrl);
        var outputFile = new FileInfo(outputPath);

        var bounds = ParseBounds(request.Bounds);
        var provider = request.ElevationProvider ?? "srtm";
        var localRoot = request.LocalPath;

        StartJobProcessing(
            jobId,
            request.WebhookUrl,
            async (job) =>
            {
                job = job with { Progress = 20 };
                await _jobRepository.UpdateJobAsync(job);

                return await _command.BoundsToObjAsync(
                    bounds: bounds,
                    output: outputFile,
                    provider: provider,
                    localRoot: localRoot,
                    srtmBaseUrl: null,
                    persistDownloads: true,
                    enableCache: true,
                    airGapped: false,
                    forceAgenticFail: false,
                    validateIntegrity: false,
                    meshQualityReport: false,
                    cacheRoot: request.CacheRoot,
                    persistCache: request.PersistCache ?? true,
                    json: false,
                    verbose: true,
                    ct: CancellationToken.None);
            },
            outputPath);

        return jobId;
    }
}
