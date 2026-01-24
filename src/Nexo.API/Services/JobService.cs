using Microsoft.Extensions.Logging;

namespace Nexo.API.Services;

/// <summary>
/// Implementation of job management service.
/// </summary>
public class JobService : IJobService
{
    private readonly ILogger<JobService> _logger;

    public JobService(ILogger<JobService> logger)
    {
        _logger = logger;
    }

    public Task<bool> CancelJobAsync(string jobId)
    {
        // TODO: Implement job cancellation
        _logger.LogInformation("Cancelling job {JobId}", jobId);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteJobAsync(string jobId)
    {
        // TODO: Implement job deletion
        _logger.LogInformation("Deleting job {JobId}", jobId);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<string>> ListJobsAsync()
    {
        // TODO: Implement job listing
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }
}
