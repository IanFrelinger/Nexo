using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using Nexo.CLI.Commands.World;
using System.Collections.Concurrent;

namespace Nexo.API.Services;

/// <summary>
/// Implementation of world bundle generation service.
/// </summary>
public class WorldService : IWorldService
{
    private readonly WorldCommand _command;
    private readonly ILogger<WorldService> _logger;
    private readonly WebhookService? _webhookService;
    private readonly ConcurrentDictionary<string, JobStatusResponse> _jobs = new();
    private readonly string _outputDirectory;

    public WorldService(
        WorldCommand command,
        ILogger<WorldService> logger,
        WebhookService? webhookService = null)
    {
        _command = command;
        _logger = logger;
        _webhookService = webhookService;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "nexo-api", "world");
        Directory.CreateDirectory(_outputDirectory);
    }

    public Task<string> GenerateWorldAsync(WorldGenerationRequest request)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var outputPath = Path.Combine(_outputDirectory, jobId);
        var outputDir = new DirectoryInfo(outputPath);
        var webhookUrl = request.WebhookUrl;

        _jobs[jobId] = new JobStatusResponse
        {
            JobId = jobId,
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        // Start async processing
        _ = Task.Run(async () =>
        {
            try
            {
                _jobs[jobId] = _jobs[jobId] with { Status = "processing", Progress = 10 };

                // Parse bounds
                var boundsParts = request.Bounds.Split(',');
                if (boundsParts.Length != 4)
                {
                    throw new ArgumentException("Bounds must be in format: minLat,maxLat,minLon,maxLon");
                }

                var bounds = $"{boundsParts[0]},{boundsParts[1]},{boundsParts[2]},{boundsParts[3]}";

                // Determine providers
                var elevationProvider = request.ElevationProvider ?? "srtm";
                var vectorProvider = request.VectorProvider ?? "hybrid";
                var mapboxToken = request.MapboxToken ?? Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");
                var osmPbfPath = request.OsmPbfPath;

                _jobs[jobId] = _jobs[jobId] with { Progress = 20 };

                // Execute world generation using CLI command
                var exitCode = await _command.BuildAsync(
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

                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"World generation failed with exit code {exitCode}");
                }

                _jobs[jobId] = _jobs[jobId] with
                {
                    Status = "completed",
                    Progress = 100,
                    OutputPath = outputPath,
                    CompletedAt = DateTime.UtcNow
                };

                _logger.LogInformation("World generation job {JobId} completed successfully", jobId);

                // Send webhook if configured
                if (!string.IsNullOrEmpty(webhookUrl) && _webhookService != null)
                {
                    await _webhookService.SendWebhookAsync(webhookUrl, jobId, "completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing world generation job {JobId}", jobId);
                _jobs[jobId] = _jobs[jobId] with
                {
                    Status = "failed",
                    ErrorMessage = ex.Message,
                    CompletedAt = DateTime.UtcNow
                };

                // Send webhook if configured
                if (!string.IsNullOrEmpty(webhookUrl) && _webhookService != null)
                {
                    await _webhookService.SendWebhookAsync(webhookUrl, jobId, "failed", ex.Message);
                }
            }
        });

        return Task.FromResult(jobId);
    }

    public Task<JobStatusResponse?> GetJobStatusAsync(string jobId)
    {
        _jobs.TryGetValue(jobId, out var status);
        return Task.FromResult(status);
    }

    public Task<string?> GetJobOutputPathAsync(string jobId, string format)
    {
        if (_jobs.TryGetValue(jobId, out var status) && status.Status == "completed")
        {
            return Task.FromResult<string?>(status.OutputPath);
        }
        return Task.FromResult<string?>(null);
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
