using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using Nexo.CLI.Commands.GeoTerrain;
using System.Collections.Concurrent;

namespace Nexo.API.Services;

/// <summary>
/// Implementation of terrain generation service.
/// </summary>
public class GeoTerrainService : IGeoTerrainService
{
    private readonly GeoTerrainCommand _command;
    private readonly ILogger<GeoTerrainService> _logger;
    private readonly WebhookService? _webhookService;
    private readonly ConcurrentDictionary<string, JobStatusResponse> _jobs = new();
    private readonly string _outputDirectory;

    public GeoTerrainService(
        GeoTerrainCommand command,
        ILogger<GeoTerrainService> logger,
        WebhookService? webhookService = null)
    {
        _command = command;
        _logger = logger;
        _webhookService = webhookService;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "nexo-api", "geoterrain");
        Directory.CreateDirectory(_outputDirectory);
    }

    public Task<string> GenerateTerrainAsync(TerrainGenerationRequest request)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var outputPath = Path.Combine(_outputDirectory, $"{jobId}.{request.Format}");
        var outputFile = new FileInfo(outputPath);
        
        // Get webhook URL from request
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

                // Determine provider
                var provider = request.ElevationProvider ?? "srtm";
                var localRoot = request.LocalPath;
                var mapboxToken = request.MapboxToken ?? Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");

                _jobs[jobId] = _jobs[jobId] with { Progress = 20 };

                // Execute terrain generation using CLI command
                var exitCode = await _command.BoundsToObjAsync(
                    bounds: bounds,
                    output: outputFile,
                    provider: provider,
                    localRoot: localRoot,
                    srtmBaseUrl: null,
                    persistDownloads: true,
                    enableCache: true,
                    airGapped: false,
                    forceAgenticFail: false,
                    json: false,
                    verbose: true,
                    ct: CancellationToken.None);

                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Terrain generation failed with exit code {exitCode}");
                }

                _jobs[jobId] = _jobs[jobId] with
                {
                    Status = "completed",
                    Progress = 100,
                    OutputPath = outputPath,
                    CompletedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Terrain generation job {JobId} completed successfully", jobId);

                // Send webhook if configured
                if (!string.IsNullOrEmpty(webhookUrl) && _webhookService != null)
                {
                    await _webhookService.SendWebhookAsync(webhookUrl, jobId, "completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing terrain generation job {JobId}", jobId);
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
}
