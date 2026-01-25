using Nexo.API.Models;

namespace Nexo.API.Services;

/// <summary>
/// Common interface for services that manage job status.
/// </summary>
public interface IJobStatusService
{
    /// <summary>
    /// Get job status.
    /// </summary>
    Task<JobStatusResponse?> GetJobStatusAsync(string jobId);

    /// <summary>
    /// Get output file path for completed job.
    /// </summary>
    Task<string?> GetJobOutputPathAsync(string jobId, string format);
}
