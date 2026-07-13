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

    public async Task ProjectBatchAsync(
        IReadOnlyList<VerifiedProvenanceRecord> records,
        ProvenanceGraphMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        await using var session = _driver.AsyncSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            foreach (var record in records)
            {
                await tx.RunAsync(
                    """
                    MERGE (a:Artifact {id: $artifactId})
                    SET a.kind = $artifactKind, a.verified = true
                    MERGE (c:Certificate {id: $certificateHash})
                    SET c.issuedAt = $issuedAt, c.signerKeyId = $signerKeyId, c.verified = true
                    """,
                    new
                    {
                        artifactId = record.ArtifactId,
                        artifactKind = record.ArtifactKind.ToString().ToLowerInvariant(),
                        certificateHash = record.CertificateHash,
                        issuedAt = record.IssuedAt?.UtcDateTime,
                        signerKeyId = record.SignerKeyId
                    }).ConfigureAwait(false);
            }

            foreach (var record in records)
            {
                await tx.RunAsync(
                    """
                    MATCH (a:Artifact {id: $artifactId, verified: true})
                    MATCH (c:Certificate {id: $certificateHash})
                    MERGE (a)-[:CERTIFIED_BY]->(c)
                    """,
                    new { artifactId = record.ArtifactId, certificateHash = record.CertificateHash }).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(record.PriorCertificateHash))
                {
                    await RequireNodeAsync(tx, "Certificate", record.PriorCertificateHash).ConfigureAwait(false);
                    await tx.RunAsync(
                        """
                        MATCH (c:Certificate {id: $certificateHash, verified: true})
                        MATCH (prior:Certificate {id: $priorHash, verified: true})
                        MERGE (c)-[:CHAINS_TO]->(prior)
                        """,
                        new
                        {
                            certificateHash = record.CertificateHash,
                            priorHash = record.PriorCertificateHash
                        }).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(record.PolicyName) && !string.IsNullOrWhiteSpace(record.PolicyVersion))
                {
                    var policyId = ProvenanceCertificateHasher.ComputePolicyVersionId(record.PolicyName, record.PolicyVersion);
                    await tx.RunAsync(
                        """
                        MATCH (c:Certificate {id: $certificateHash, verified: true})
                        MERGE (p:PolicyVersion {id: $policyId})
                        SET p.name = $policyName, p.version = $policyVersion, p.verified = true
                        MERGE (c)-[:ISSUED_UNDER]->(p)
                        """,
                        new
                        {
                            certificateHash = record.CertificateHash,
                            policyId,
                            policyName = record.PolicyName,
                            policyVersion = record.PolicyVersion
                        }).ConfigureAwait(false);
                }

                if (!string.IsNullOrWhiteSpace(record.ProducerAgentId) && record.ProducerAgentKind.HasValue)
                {
                    await tx.RunAsync(
                        """
                        MATCH (a:Artifact {id: $artifactId, verified: true})
                        MERGE (g:Agent {id: $agentId})
                        SET g.kind = $agentKind, g.verified = true
                        MERGE (a)-[:PRODUCED_BY]->(g)
                        """,
                        new
                        {
                            artifactId = record.ArtifactId,
                            agentId = record.ProducerAgentId,
                            agentKind = record.ProducerAgentKind.Value.ToString().ToLowerInvariant()
                        }).ConfigureAwait(false);
                }

                foreach (var dependency in record.DependsOnArtifactIds)
                {
                    await RequireNodeAsync(tx, "Artifact", dependency).ConfigureAwait(false);
                    await tx.RunAsync(
                        """
                        MATCH (a:Artifact {id: $artifactId, verified: true})
                        MATCH (dep:Artifact {id: $depId, verified: true})
                        MERGE (a)-[:DEPENDS_ON]->(dep)
                        """,
                        new { artifactId = record.ArtifactId, depId = dependency }).ConfigureAwait(false);
                }
            }

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

    private static async Task RequireNodeAsync(IAsyncQueryRunner tx, string label, string id)
    {
        var query = label switch
        {
            "Artifact" => "MATCH (n:Artifact {id: $id, verified: true}) RETURN count(n) AS count",
            "Certificate" => "MATCH (n:Certificate {id: $id, verified: true}) RETURN count(n) AS count",
            _ => throw new ArgumentOutOfRangeException(nameof(label))
        };

        var cursor = await tx.RunAsync(query, new { id }).ConfigureAwait(false);
        var record = await cursor.SingleAsync().ConfigureAwait(false);
        if (record["count"].As<long>() != 1)
        {
            throw new InvalidOperationException(
                $"Cannot project relationship to unverified {label.ToLowerInvariant()} '{id}'.");
        }
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
