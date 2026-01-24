using Nexo.API.Models;

namespace Nexo.API.Services;

/// <summary>
/// Service for world bundle generation operations.
/// </summary>
public interface IWorldService
{
    /// <summary>
    /// Generate world bundle asynchronously.
    /// </summary>
    Task<string> GenerateWorldAsync(WorldGenerationRequest request);

    /// <summary>
    /// Get job status.
    /// </summary>
    Task<JobStatusResponse?> GetJobStatusAsync(string jobId);

    /// <summary>
    /// Get output file path for completed job.
    /// </summary>
    Task<string?> GetJobOutputPathAsync(string jobId, string format);

    /// <summary>
    /// Validate world bundle.
    /// </summary>
    Task<ValidationResult> ValidateWorldAsync(string bundlePath);
}

/// <summary>
/// Validation result.
/// </summary>
public record ValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<string> Issues { get; init; }
}
