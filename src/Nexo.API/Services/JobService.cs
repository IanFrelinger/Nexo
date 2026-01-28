using Microsoft.Extensions.Logging;
using Nexo.API.Models;

namespace Nexo.API.Services;

/// <summary>
/// Implementation of job management service.
/// Provides high-level job operations using the job repository.
/// </summary>
public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;
    private readonly ILogger<JobService> _logger;

    public JobService(IJobRepository jobRepository, ILogger<JobService> logger)
    {
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _logger = logger;
    }

    public async Task<bool> CancelJobAsync(string jobId)
    {
        try
        {
            var job = await _jobRepository.GetJobAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found for cancellation", jobId);
                return false;
            }

            // Only cancel jobs that are in progress or pending
            if (job.Status != "completed" && job.Status != "failed" && job.Status != "cancelled")
            {
                var cancelledJob = job with
                {
                    Status = "cancelled",
                    CompletedAt = DateTime.UtcNow,
                    ErrorMessage = "Job cancelled by user"
                };

                await _jobRepository.UpdateJobAsync(cancelledJob);
                _logger.LogInformation("Job {JobId} cancelled successfully", jobId);
                return true;
            }

            _logger.LogInformation("Job {JobId} cannot be cancelled (status: {Status})", jobId, job.Status);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);
            return false;
        }
    }

    public async Task<bool> DeleteJobAsync(string jobId)
    {
        try
        {
            var job = await _jobRepository.GetJobAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found for deletion", jobId);
                return false;
            }

            // Optionally delete output files if they exist
            if (!string.IsNullOrEmpty(job.OutputPath) && File.Exists(job.OutputPath))
            {
                try
                {
                    File.Delete(job.OutputPath);
                    _logger.LogInformation("Deleted output file for job {JobId}: {OutputPath}", jobId, job.OutputPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete output file for job {JobId}: {OutputPath}", jobId, job.OutputPath);
                    // Continue with job deletion even if file deletion fails
                }
            }

            await _jobRepository.DeleteJobAsync(jobId);
            _logger.LogInformation("Job {JobId} deleted successfully", jobId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete job {JobId}", jobId);
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> ListJobsAsync()
    {
        try
        {
            var jobs = await _jobRepository.GetJobsAsync(limit: 1000);
            return jobs.Select(j => j.JobId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list jobs");
            return Array.Empty<string>();
        }
    }
}
