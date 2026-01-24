using Nexo.API.Models;

namespace Nexo.API.Services;

/// <summary>
/// Service for terrain generation operations.
/// </summary>
public interface IGeoTerrainService
{
    /// <summary>
    /// Generate terrain mesh asynchronously.
    /// </summary>
    Task<string> GenerateTerrainAsync(TerrainGenerationRequest request);

    /// <summary>
    /// Get job status.
    /// </summary>
    Task<JobStatusResponse?> GetJobStatusAsync(string jobId);

    /// <summary>
    /// Get output file path for completed job.
    /// </summary>
    Task<string?> GetJobOutputPathAsync(string jobId, string format);
}
