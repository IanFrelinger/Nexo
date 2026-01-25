using Nexo.API.Models;

namespace Nexo.API.Services;

/// <summary>
/// Service for world bundle generation operations.
/// </summary>
public interface IWorldService : IJobStatusService
{
    /// <summary>
    /// Generate world bundle asynchronously.
    /// </summary>
    Task<string> GenerateWorldAsync(WorldGenerationRequest request);

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
