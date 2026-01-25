using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using Nexo.GeoTerrain;

namespace Nexo.API.Services;

/// <summary>
/// Base class for geospatial services that handles common job management patterns.
/// </summary>
public abstract class BaseGeospatialService<TCommand>
{
    protected readonly TCommand _command;
    protected readonly ILogger _logger;
    protected readonly WebhookService? _webhookService;
    protected readonly IJobRepository _jobRepository;
    protected readonly string _outputDirectory;

    protected BaseGeospatialService(
        TCommand command,
        ILogger logger,
        IJobRepository jobRepository,
        WebhookService? webhookService,
        string outputDirectoryName)
    {
        _command = command;
        _logger = logger;
        _jobRepository = jobRepository;
        _webhookService = webhookService;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "nexo-api", outputDirectoryName);
        Directory.CreateDirectory(_outputDirectory);
    }

    /// <summary>
    /// Parses bounds string into a normalized format.
    /// </summary>
    protected static string ParseBounds(string bounds)
    {
        var boundsParts = bounds.Split(',');
        if (boundsParts.Length != 4)
        {
            throw new ArgumentException("Bounds must be in format: minLat,maxLat,minLon,maxLon");
        }
        return $"{boundsParts[0]},{boundsParts[1]},{boundsParts[2]},{boundsParts[3]}";
    }

    /// <summary>
    /// Creates a new job and returns its ID and output path.
    /// </summary>
    protected async Task<(string jobId, string outputPath)> CreateJobAsync(string format, string? webhookUrl = null, bool isDirectory = false)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var outputPath = isDirectory 
            ? Path.Combine(_outputDirectory, jobId)
            : Path.Combine(_outputDirectory, $"{jobId}.{format}");
        
        var job = new JobStatusResponse
        {
            JobId = jobId,
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _jobRepository.CreateJobAsync(job);
        return (jobId, outputPath);
    }

    /// <summary>
    /// Starts async job processing with error handling and webhook notifications.
    /// </summary>
    protected void StartJobProcessing(
        string jobId,
        string? webhookUrl,
        Func<JobStatusResponse, Task<int>> executeCommand,
        string outputPath)
    {
        _ = Task.Run(async () =>
        {
            var job = await _jobRepository.GetJobAsync(jobId) 
                ?? throw new InvalidOperationException($"Job {jobId} not found");

            try
            {
                job = job with { Status = "processing", Progress = 10 };
                await _jobRepository.UpdateJobAsync(job);

                var exitCode = await executeCommand(job);

                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Command failed with exit code {exitCode}");
                }

                job = job with
                {
                    Status = "completed",
                    Progress = 100,
                    OutputPath = outputPath,
                    CompletedAt = DateTime.UtcNow
                };
                await _jobRepository.UpdateJobAsync(job);

                _logger.LogInformation("Job {JobId} completed successfully", jobId);

                if (!string.IsNullOrEmpty(webhookUrl) && _webhookService != null)
                {
                    await _webhookService.SendWebhookAsync(webhookUrl, jobId, "completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job {JobId}", jobId);
                job = job with
                {
                    Status = "failed",
                    ErrorMessage = ex.Message,
                    CompletedAt = DateTime.UtcNow
                };
                await _jobRepository.UpdateJobAsync(job);

                if (!string.IsNullOrEmpty(webhookUrl) && _webhookService != null)
                {
                    await _webhookService.SendWebhookAsync(webhookUrl, jobId, "failed", ex.Message);
                }
            }
        });
    }

    /// <summary>
    /// Gets job status from repository.
    /// </summary>
    public async Task<JobStatusResponse?> GetJobStatusAsync(string jobId)
    {
        return await _jobRepository.GetJobAsync(jobId);
    }

    /// <summary>
    /// Gets job output path if job is completed.
    /// </summary>
    public async Task<string?> GetJobOutputPathAsync(string jobId, string format)
    {
        var status = await _jobRepository.GetJobAsync(jobId);
        if (status != null && status.Status == "completed")
        {
            return status.OutputPath;
        }
        return null;
    }
}
