using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using Nexo.CLI.Commands.World;

namespace Nexo.API.Services;

/// <summary>
/// Implementation of world bundle generation service.
/// </summary>
public class WorldService : BaseGeospatialService<IWorldCommand>, IWorldService
{
    public WorldService(
        IWorldCommand command,
        ILogger<WorldService> logger,
        IJobRepository jobRepository,
        WebhookService? webhookService = null)
        : base(command, logger, jobRepository, webhookService, "world")
    {
    }

    public async Task<string> GenerateWorldAsync(WorldGenerationRequest request)
    {
        var (jobId, outputPath) = await CreateJobAsync("zip", request.WebhookUrl, isDirectory: true);
        var outputDir = new DirectoryInfo(outputPath);

        var bounds = ParseBounds(request.Bounds);
        var elevationProvider = request.ElevationProvider ?? "srtm";
        var vectorProvider = request.VectorProvider ?? "hybrid";
        var mapboxToken = request.MapboxToken ?? Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");
        var osmPbfPath = request.OsmPbfPath;

        StartJobProcessing(
            jobId,
            request.WebhookUrl,
            async (job) =>
            {
                job = job with { Progress = 20 };
                await _jobRepository.UpdateJobAsync(job);

                return await _command.BuildAsync(
                    bounds: bounds,
                    outDir: outputDir,
                    terrainElevationProvider: elevationProvider,
                    terrainLocalRoot: null,
                    terrainSrtmBaseUrl: null,
                    terrainPersistDownloads: true,
                    terrainEnableCache: true,
                    vectorProvider: vectorProvider,
                    osmPbfPath: osmPbfPath,
                    mapboxAccessToken: mapboxToken,
                    mapboxTileset: null,
                    mapboxZoom: null,
                    terrainChunkSamples: 256,
                    terrainLodFactors: null,
                    lodTriBudgets: null,
                    instancesChunkSamples: 256,
                    enableTerrainImagery: false,
                    terrainImageryTileset: null,
                    terrainImageryFormat: null,
                    terrainImageryZoom: null,
                    waterFlattenToTerrain: true,
                    meshFormat: request.Format,
                    projection: "utm",
                    generateVectorTextures: false,
                    airGapped: false,
                    json: false,
                    verbose: true,
                    ct: CancellationToken.None);
            },
            outputPath);

        return jobId;
    }

    public async Task<ValidationResult> ValidateWorldAsync(string bundlePath)
    {
        try
        {
            var bundleDir = new DirectoryInfo(bundlePath);
            if (!bundleDir.Exists)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Issues = new[] { $"Bundle directory does not exist: {bundlePath}" }
                };
            }

            // Execute validation using CLI command
            var exitCode = await _command.ValidateAsync(
                bundleDir: bundleDir,
                json: false,
                verbose: true,
                ct: CancellationToken.None);

            // Parse validation output (WorldCommand.ValidateAsync writes to console)
            // For now, assume exit code 0 means valid
            var issues = new List<string>();
            if (exitCode != 0)
            {
                issues.Add($"Validation failed with exit code {exitCode}");
            }

            return new ValidationResult
            {
                IsValid = issues.Count == 0,
                Issues = issues
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating world bundle");
            return new ValidationResult
            {
                IsValid = false,
                Issues = new[] { ex.Message }
            };
        }
    }
}
