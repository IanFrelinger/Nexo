using Nexo.API.Models;

namespace Nexo.API.Services;

/// <summary>
/// Service for vector feature extraction operations.
/// </summary>
public interface IGeoVectorService : IJobStatusService
{
    /// <summary>
    /// Extract vector features asynchronously.
    /// </summary>
    Task<string> ExtractFeaturesAsync(VectorExtractionRequest request);
}
