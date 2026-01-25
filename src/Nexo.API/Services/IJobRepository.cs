using Nexo.API.Models;

namespace Nexo.API.Services;

/// <summary>
/// Repository for persistent job storage.
/// </summary>
public interface IJobRepository
{
    Task<string> CreateJobAsync(JobStatusResponse job, CancellationToken ct = default);
    Task<JobStatusResponse?> GetJobAsync(string jobId, CancellationToken ct = default);
    Task UpdateJobAsync(JobStatusResponse job, CancellationToken ct = default);
    Task<IReadOnlyList<JobStatusResponse>> GetJobsAsync(int limit = 100, CancellationToken ct = default);
    Task DeleteJobAsync(string jobId, CancellationToken ct = default);
    Task DeleteOldJobsAsync(TimeSpan olderThan, CancellationToken ct = default);
}
