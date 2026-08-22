using Ashlar.Core.Application.Maintenance.Models;

namespace Ashlar.Core.Application.Maintenance.Ports;

/// <summary>
/// Port for artifact cleanup strategies. Each strategy handles one type of disk-hogging artifact
/// (e.g. test artifacts, incomplete blobs in content-addressed storage).
/// Implemented by Infrastructure adapters.
/// </summary>
public interface IArtifactCleanupStrategy
{
    /// <summary>Unique strategy identifier (e.g. "test-artifacts", "incomplete-blobs").</summary>
    string Id { get; }

    /// <summary>Human-readable description of what this strategy cleans.</summary>
    string Description { get; }

    /// <summary>Executes cleanup for this strategy.</summary>
    /// <param name="context">Context with repo root and strategy-specific options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result with bytes reclaimed, paths deleted, and any errors.</returns>
    Task<ArtifactCleanupResult> CleanAsync(ArtifactCleanupContext context, CancellationToken ct = default);
}
