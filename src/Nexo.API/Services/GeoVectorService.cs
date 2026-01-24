using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using Nexo.CLI.Commands.GeoVector;
using System.Collections.Concurrent;

namespace Nexo.API.Services;

/// <summary>
/// Implementation of vector feature extraction service.
/// </summary>
public class GeoVectorService : IGeoVectorService
{
    private readonly GeoVectorCommand _command;
    private readonly ILogger<GeoVectorService> _logger;
    private readonly WebhookService? _webhookService;
    private readonly ConcurrentDictionary<string, JobStatusResponse> _jobs = new();
    private readonly string _outputDirectory;

    public GeoVectorService(
        GeoVectorCommand command,
        ILogger<GeoVectorService> logger,
        WebhookService? webhookService = null)
    {
        _command = command;
        _logger = logger;
        _webhookService = webhookService;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "nexo-api", "geovector");
        Directory.CreateDirectory(_outputDirectory);
    }

    public Task<string> ExtractFeaturesAsync(VectorExtractionRequest request)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var outputPath = Path.Combine(_outputDirectory, $"{jobId}.{request.Format}");
        var outputFile = new FileInfo(outputPath);
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
                var provider = request.VectorProvider ?? "hybrid";
                var mapboxToken = request.MapboxToken ?? Environment.GetEnvironmentVariable("MAPBOX_ACCESS_TOKEN");
                var osmPbfPath = request.OsmPbfPath ?? request.VectorFilePath;

                _jobs[jobId] = _jobs[jobId] with { Progress = 20 };

                // Execute vector extraction based on feature kind
                int exitCode;
                if (string.Equals(request.FeatureKind, "building", StringComparison.OrdinalIgnoreCase))
                {
                    exitCode = await _command.BuildingsToObjAsync(
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
                        json: false,
                        verbose: true,
                        ct: CancellationToken.None);
                }
                else
                {
                    // For other feature kinds, use ExtractAsync if available
                    // For now, throw not implemented
                    throw new NotImplementedException($"Feature kind '{request.FeatureKind}' extraction not yet implemented in API service");
                }

                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Vector extraction failed with exit code {exitCode}");
                }

                _jobs[jobId] = _jobs[jobId] with
                {
                    Status = "completed",
                    Progress = 100,
                    OutputPath = outputPath,
                    CompletedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Vector extraction job {JobId} completed successfully", jobId);

                // Send webhook if configured
                if (!string.IsNullOrEmpty(webhookUrl) && _webhookService != null)
                {
                    await _webhookService.SendWebhookAsync(webhookUrl, jobId, "completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing vector extraction job {JobId}", jobId);
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
