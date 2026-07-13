using Nexo.Provenance.Graph.Models;

namespace Nexo.Provenance.Graph.Ports;

/// <summary>
/// Read-write provenance graph store. Neo4j is optional; default is a no-op implementation.
/// </summary>
public interface IProvenanceGraphStore
{
    /// <summary>Whether this store persists graph data (false for null/no-op).</summary>
    bool IsEnabled { get; }

    /// <summary>Apply schema constraints (idempotent).</summary>
    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically project a fully verified batch and its chain-head metadata using MERGE semantics.
    /// Implementations must leave the graph unchanged if any operation fails.
    /// </summary>
    Task ProjectBatchAsync(
        IReadOnlyList<VerifiedProvenanceRecord> records,
        ProvenanceGraphMetadata metadata,
        CancellationToken cancellationToken = default);

    /// <summary>Read current graph metadata.</summary>
    Task<ProvenanceGraphMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>Clear all graph data (testing only).</summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>Capture in-memory snapshot (supported by test store; optional elsewhere).</summary>
    Task<ProvenanceGraphSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
