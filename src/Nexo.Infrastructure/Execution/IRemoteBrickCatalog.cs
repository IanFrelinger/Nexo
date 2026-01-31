using Nexo.BrickContracts;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Fetches brick catalog metadata from a remote brick host (GET /api/bricks).
/// </summary>
public interface IRemoteBrickCatalog
{
    /// <summary>Base URL of the brick host (e.g. https://nexo.example.com).</summary>
    string BaseUrl { get; }

    /// <summary>Get all brick metadata from the catalog.</summary>
    Task<IReadOnlyList<BrickCatalogEntryDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Get one brick's metadata by id, or null if not found.</summary>
    Task<BrickCatalogEntryDto?> GetByIdAsync(string brickId, CancellationToken cancellationToken = default);
}
