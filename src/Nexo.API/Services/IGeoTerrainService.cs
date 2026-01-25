using Nexo.API.Models;

namespace Nexo.API.Services;

/// <summary>
/// Service for terrain generation operations.
/// </summary>
public interface IGeoTerrainService : IJobStatusService
{
    /// <summary>
    /// Generate terrain mesh asynchronously.
    /// </summary>
    Task<string> GenerateTerrainAsync(TerrainGenerationRequest request);
}
