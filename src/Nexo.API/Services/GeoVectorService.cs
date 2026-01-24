using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using System.Collections.Concurrent;

namespace Nexo.API.Services;

/// <summary>
/// Implementation of vector feature extraction service.
/// </summary>
public class GeoVectorService : IGeoVectorService
{
    private readonly ILogger<GeoVectorService> _logger;
    private readonly ConcurrentDictionary<string, JobStatusResponse> _jobs = new();
    private readonly string _outputDirectory;

    public GeoVectorService(ILogger<GeoVectorService> logger)
    {
        _logger = logger;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "nexo-api", "geovector");
        Directory.CreateDirectory(_outputDirectory);
    }

    public Task<string> ExtractFeaturesAsync(VectorExtractionRequest request)
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

                // TODO: Integrate with actual GeoVectorCommand execution
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
                _logger.LogError(ex, "Error processing vector extraction job {JobId}", jobId);
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
