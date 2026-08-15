using Neo4j.Driver;
using Nexo.Provenance.Graph.Models;
using Nexo.Provenance.Graph.Ports;

namespace Nexo.Provenance.Graph.Neo4j;

/// <summary>Neo4j Cypher queries for provenance lineage with fail-closed staleness checks.</summary>
public sealed class Neo4jProvenanceGraphQueries : IProvenanceGraphQueries
{
    private readonly IDriver _driver;
    private readonly IProvenanceChainHeadAuthority _chainHeadAuthority;

    public Neo4jProvenanceGraphQueries(
        IDriver driver,
        IProvenanceChainHeadAuthority chainHeadAuthority)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _chainHeadAuthority = chainHeadAuthority ?? throw new ArgumentNullException(nameof(chainHeadAuthority));
    }

    public async Task<LineageQueryResult> LineageOfAsync(string artifactId, CancellationToken cancellationToken = default)
    {
        var chainHead = await RequireFreshChainHeadAsync(cancellationToken).ConfigureAwait(false);
        await using var session = _driver.AsyncSession();

        var certCursor = await session.RunAsync(
            """
            MATCH (a:Artifact {id: $artifactId})-[:CERTIFIED_BY]->(c:Certificate)
            OPTIONAL MATCH chain = (c)-[:CHAINS_TO*0..]->(root:Certificate)
            WITH c, chain
            ORDER BY length(chain) DESC
            LIMIT 1
            WITH reverse(nodes(chain)) AS certs
            UNWIND range(0, size(certs) - 1) AS position
            WITH certs[position] AS cert, position
            OPTIONAL MATCH (cert)-[:ISSUED_UNDER]->(p:PolicyVersion)
            RETURN DISTINCT cert.id AS certHash,
                   cert.issuedAt AS issuedAt,
                   cert.signerKeyId AS signerKeyId,
                   p.name AS policyName,
                   p.version AS policyVersion,
                   position
            ORDER BY position
            """,
            new { artifactId }).ConfigureAwait(false);

        var certificates = new List<LineageCertificateEntry>();
        await foreach (var record in certCursor.ConfigureAwait(false))
        {
            certificates.Add(new LineageCertificateEntry
            {
                CertificateHash = record["certHash"].As<string>(),
                IssuedAt = record["issuedAt"] is null
                    ? null
                    : DateTime.SpecifyKind(record["issuedAt"].As<DateTime>(), DateTimeKind.Utc),
                SignerKeyId = record["signerKeyId"].As<string>(),
                PolicyId = record["policyName"] as string,
                PolicyVersion = record["policyVersion"] as string
            });
        }

        var depCursor = await session.RunAsync(
            """
            MATCH (a:Artifact {id: $artifactId})-[:DEPENDS_ON]->(dep:Artifact)
            RETURN dep.id AS depId
            ORDER BY depId
            """,
            new { artifactId }).ConfigureAwait(false);

        var deps = new List<string>();
        await foreach (var record in depCursor.ConfigureAwait(false))
            deps.Add(record["depId"].As<string>());

        return new LineageQueryResult
        {
            ChainHeadHash = chainHead,
            ArtifactId = artifactId,
            Certificates = certificates,
            DependencyArtifactIds = deps
        };
    }

    public async Task<ArtifactsUnderPolicyResult> ArtifactsUnderPolicyAsync(string policyId, string policyVersion, CancellationToken cancellationToken = default)
    {
        var chainHead = await RequireFreshChainHeadAsync(cancellationToken).ConfigureAwait(false);
        var policyNodeId = $"{policyId}@{policyVersion}";
        await using var session = _driver.AsyncSession();

        var cursor = await session.RunAsync(
            """
            MATCH (p:PolicyVersion {id: $policyNodeId})<-[:ISSUED_UNDER]-(seed:Certificate)
            OPTIONAL MATCH (c:Certificate)-[:CHAINS_TO*0..]->(seed)
            WITH collect(DISTINCT c) + collect(DISTINCT seed) AS certs
            UNWIND certs AS cert
            MATCH (a:Artifact)-[:CERTIFIED_BY]->(cert)
            RETURN DISTINCT a.id AS artifactId
            ORDER BY artifactId
            """,
            new { policyNodeId }).ConfigureAwait(false);

        var artifactIds = new List<string>();
        await foreach (var record in cursor.ConfigureAwait(false))
            artifactIds.Add(record["artifactId"].As<string>());

        return new ArtifactsUnderPolicyResult
        {
            ChainHeadHash = chainHead,
            PolicyId = policyId,
            PolicyVersion = policyVersion,
            ArtifactIds = artifactIds
        };
    }

    public async Task<BlastRadiusResult> BlastRadiusOfAsync(string policyId, string policyVersion, CancellationToken cancellationToken = default)
    {
        var underPolicy = await ArtifactsUnderPolicyAsync(policyId, policyVersion, cancellationToken)
            .ConfigureAwait(false);

        await using var session = _driver.AsyncSession();
        var cursor = await session.RunAsync(
            """
            UNWIND $seedIds AS seedId
            MATCH (seed:Artifact {id: seedId})
            MATCH (a:Artifact)-[:DEPENDS_ON*1..]->(seed)
            RETURN DISTINCT a.id AS artifactId
            ORDER BY artifactId
            """,
            new { seedIds = underPolicy.ArtifactIds }).ConfigureAwait(false);

        var affected = new HashSet<string>(underPolicy.ArtifactIds, StringComparer.Ordinal);
        await foreach (var record in cursor.ConfigureAwait(false))
            affected.Add(record["artifactId"].As<string>());

        return new BlastRadiusResult
        {
            ChainHeadHash = underPolicy.ChainHeadHash,
            PolicyId = policyId,
            PolicyVersion = policyVersion,
            AffectedArtifactIds = affected.OrderBy(id => id, StringComparer.Ordinal).ToList()
        };
    }

    private async Task<string> RequireFreshChainHeadAsync(CancellationToken cancellationToken)
    {
        var currentChainHeadHash = await _chainHeadAuthority
            .GetCurrentChainHeadHashAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var session = _driver.AsyncSession();
        var cursor = await session.RunAsync(
            """
            MATCH (m:GraphMetadata {id: 'provenance'})
            RETURN m.chainHeadHash AS chainHeadHash
            """).ConfigureAwait(false);

        var records = await cursor.ToListAsync().ConfigureAwait(false);
        if (records.Count == 0)
            throw new ProvenanceGraphStaleException(string.Empty, currentChainHeadHash);

        var graphHead = records[0]["chainHeadHash"].As<string>();
        if (!string.Equals(graphHead, currentChainHeadHash, StringComparison.Ordinal))
            throw new ProvenanceGraphStaleException(graphHead, currentChainHeadHash);

        return graphHead;
    }
}
