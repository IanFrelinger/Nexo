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
    private readonly ConcurrentDictionary<string, JobStatusResponse> _jobs = new();
    private readonly string _outputDirectory;

    public GeoTerrainService(
        GeoTerrainCommand command,
        ILogger<GeoTerrainService> logger)
    {
        _command = command;
        _logger = logger;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "nexo-api", "geoterrain");
        Directory.CreateDirectory(_outputDirectory);
    }

    public Task<string> GenerateTerrainAsync(TerrainGenerationRequest request)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var outputPath = Path.Combine(_outputDirectory, $"{jobId}.{request.Format}");

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

                // Execute terrain generation (simplified - would need to adapt CLI command)
                _jobs[jobId] = _jobs[jobId] with { Progress = 50 };

                // TODO: Integrate with actual GeoTerrainCommand execution
                // For now, mark as completed
                await Task.Delay(1000); // Simulate processing

                _jobs[jobId] = _jobs[jobId] with
                {
                    Status = "completed",
                    Progress = 100,
                    OutputPath = outputPath,
                    CompletedAt = DateTime.UtcNow
                };
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
