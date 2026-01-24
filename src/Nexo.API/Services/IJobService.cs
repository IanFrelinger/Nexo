namespace Nexo.API.Services;

/// <summary>
/// Service for job management operations.
/// </summary>
public interface IJobService
{
    /// <summary>
    /// Cancel a job.
    /// </summary>
    Task<bool> CancelJobAsync(string jobId);

    /// <summary>
    /// Delete a job and its outputs.
    /// </summary>
    Task<bool> DeleteJobAsync(string jobId);

    /// <summary>
    /// List all jobs.
    /// </summary>
    Task<IReadOnlyList<string>> ListJobsAsync();
}
