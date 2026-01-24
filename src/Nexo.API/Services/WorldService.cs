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
    private readonly ConcurrentDictionary<string, JobStatusResponse> _jobs = new();
    private readonly string _outputDirectory;

    public WorldService(
        WorldCommand command,
        ILogger<WorldService> logger)
    {
        _command = command;
        _logger = logger;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "nexo-api", "world");
        Directory.CreateDirectory(_outputDirectory);
    }

    public Task<string> GenerateWorldAsync(WorldGenerationRequest request)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var outputPath = Path.Combine(_outputDirectory, jobId);

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

                // TODO: Integrate with actual WorldCommand execution
                await Task.Delay(2000); // Simulate processing

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
                _logger.LogError(ex, "Error processing world generation job {JobId}", jobId);
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

    public Task<ValidationResult> ValidateWorldAsync(string bundlePath)
    {
        try
        {
            // TODO: Integrate with actual WorldCommand.ValidateAsync
            var issues = new List<string>();
            
            if (!Directory.Exists(bundlePath))
            {
                issues.Add($"Bundle directory does not exist: {bundlePath}");
            }

            return Task.FromResult(new ValidationResult
            {
                IsValid = issues.Count == 0,
                Issues = issues
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating world bundle");
            return Task.FromResult(new ValidationResult
            {
                IsValid = false,
                Issues = new[] { ex.Message }
            });
        }
    }
}
