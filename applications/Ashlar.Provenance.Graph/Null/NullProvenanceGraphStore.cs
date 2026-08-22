using Ashlar.Provenance.Graph.Hashing;
using Ashlar.Provenance.Graph.Models;
using Ashlar.Provenance.Graph.Ports;

namespace Ashlar.Provenance.Graph.Null;

/// <summary>No-op provenance graph store — default when Neo4j is not configured.</summary>
public sealed class NullProvenanceGraphStore : IProvenanceGraphStore
{
    public bool IsEnabled => false;

    public Task EnsureSchemaAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ProjectBatchAsync(
        IReadOnlyList<VerifiedProvenanceRecord> records,
        ProvenanceGraphMetadata metadata,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<ProvenanceGraphMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ProvenanceGraphMetadata?>(null);

    public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<ProvenanceGraphSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ProvenanceGraphSnapshot?>(null);
}
