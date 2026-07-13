using Neo4j.Driver;
using Nexo.Provenance.Graph.Hashing;
using Nexo.Provenance.Graph.Models;
using Nexo.Provenance.Graph.Ports;

namespace Nexo.Provenance.Graph.Neo4j;

/// <summary>Neo4j-backed provenance graph store using parameterized Cypher and MERGE semantics.</summary>
public sealed class Neo4jProvenanceGraphStore : IProvenanceGraphStore, IAsyncDisposable
{
    private readonly IDriver _driver;

    public Neo4jProvenanceGraphStore(IDriver driver) =>
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));

    public bool IsEnabled => true;

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        await using var session = _driver.AsyncSession();
        await Neo4jSchemaMigrator.ApplyAsync(session, cancellationToken).ConfigureAwait(false);
    }

    public async Task ProjectBundleAsync(ProvenanceCertificateBundle bundle, string certificateHash, CancellationToken cancellationToken = default)
    {
        var signerKeyId = ProvenanceCertificateHasher.ComputeSignerKeyId(bundle.IssuerPublicKey);
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(
                """
                MERGE (a:Artifact {id: $artifactId})
                SET a.kind = $artifactKind
                MERGE (c:Certificate {id: $certificateHash})
                SET c.issuedAt = $issuedAt, c.signerKeyId = $signerKeyId
                MERGE (a)-[:CERTIFIED_BY]->(c)
                """,
                new
                {
                    artifactId = bundle.ArtifactId,
                    artifactKind = bundle.ArtifactKind.ToString().ToLowerInvariant(),
                    certificateHash,
                    issuedAt = bundle.IssuedAt.UtcDateTime,
                    signerKeyId
                }).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(bundle.PriorCertificateHash))
            {
                await tx.RunAsync(
                    """
                    MATCH (c:Certificate {id: $certificateHash})
                    MERGE (prior:Certificate {id: $priorHash})
                    MERGE (c)-[:CHAINS_TO]->(prior)
                    """,
                    new { certificateHash, priorHash = bundle.PriorCertificateHash }).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(bundle.PolicyName) && !string.IsNullOrWhiteSpace(bundle.PolicyVersion))
            {
                var policyId = ProvenanceCertificateHasher.ComputePolicyVersionId(bundle.PolicyName, bundle.PolicyVersion);
                await tx.RunAsync(
                    """
                    MATCH (c:Certificate {id: $certificateHash})
                    MERGE (p:PolicyVersion {id: $policyId})
                    SET p.name = $policyName, p.version = $policyVersion
                    MERGE (c)-[:ISSUED_UNDER]->(p)
                    """,
                    new
                    {
                        certificateHash,
                        policyId,
                        policyName = bundle.PolicyName,
                        policyVersion = bundle.PolicyVersion
                    }).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(bundle.ProducerAgentId) && bundle.ProducerAgentKind.HasValue)
            {
                await tx.RunAsync(
                    """
                    MATCH (a:Artifact {id: $artifactId})
                    MERGE (g:Agent {id: $agentId})
                    SET g.kind = $agentKind
                    MERGE (a)-[:PRODUCED_BY]->(g)
                    """,
                    new
                    {
                        artifactId = bundle.ArtifactId,
                        agentId = bundle.ProducerAgentId,
                        agentKind = bundle.ProducerAgentKind.Value.ToString().ToLowerInvariant()
                    }).ConfigureAwait(false);
            }

            foreach (var dep in bundle.DependsOnArtifactIds)
            {
                await tx.RunAsync(
                    """
                    MATCH (a:Artifact {id: $artifactId})
                    MERGE (dep:Artifact {id: $depId})
                    MERGE (a)-[:DEPENDS_ON]->(dep)
                    """,
                    new { artifactId = bundle.ArtifactId, depId = dep }).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    public async Task SetMetadataAsync(ProvenanceGraphMetadata metadata, CancellationToken cancellationToken = default)
    {
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(
                """
                MERGE (m:GraphMetadata {id: 'provenance'})
                SET m.chainHeadHash = $chainHeadHash, m.projectedAt = $projectedAt
                """,
                new
                {
                    chainHeadHash = metadata.ChainHeadHash,
                    projectedAt = metadata.ProjectedAt.UtcDateTime
                }).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task<ProvenanceGraphMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        await using var session = _driver.AsyncSession();
        var cursor = await session.RunAsync(
            """
            MATCH (m:GraphMetadata {id: 'provenance'})
            RETURN m.chainHeadHash AS chainHeadHash, m.projectedAt AS projectedAt
            """).ConfigureAwait(false);

        var records = await cursor.ToListAsync().ConfigureAwait(false);
        if (records.Count == 0)
            return null;

        var record = records[0];
        return new ProvenanceGraphMetadata
        {
            ChainHeadHash = record["chainHeadHash"].As<string>(),
            ProjectedAt = DateTime.SpecifyKind(record["projectedAt"].As<DateTime>(), DateTimeKind.Utc)
        };
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync("MATCH (n) DETACH DELETE n").ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public Task<ProvenanceGraphSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ProvenanceGraphSnapshot?>(null);

    public ValueTask DisposeAsync() => _driver.DisposeAsync();
}
