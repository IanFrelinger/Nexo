using Nexo.API.Models;

namespace Nexo.API.Services;

/// <summary>
/// Service for vector feature extraction operations.
/// </summary>
public interface IGeoVectorService
{
    /// <summary>
    /// Extract vector features asynchronously.
    /// </summary>
    Task<string> ExtractFeaturesAsync(VectorExtractionRequest request);

    /// <summary>
    /// Get job status.
    /// </summary>
    Task<JobStatusResponse?> GetJobStatusAsync(string jobId);

    /// <summary>
    /// Get output file path for completed job.
    /// </summary>
    Task<string?> GetJobOutputPathAsync(string jobId, string format);
}
