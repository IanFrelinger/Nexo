using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Neo4j.Driver;
using Ashlar.Provenance.Graph.Ingestion;
using Ashlar.Provenance.Graph.Models;
using Ashlar.Provenance.Graph.Neo4j;
using Ashlar.Provenance.Graph.Ports;
using Ashlar.Provenance.Graph.Tests.Witness;
using Testcontainers.Neo4j;
using Xunit;
using Xunit.Abstractions;

namespace Ashlar.Provenance.Graph.Tests.Integration;

[Collection("Neo4jProvenance")]
[Trait("Category", "Integration")]
public sealed class Neo4jProvenanceGraphIntegrationTests
{
    private readonly Neo4jProvenanceDockerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public Neo4jProvenanceGraphIntegrationTests(Neo4jProvenanceDockerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Neo4j_ProjectAndQuery_ArtifactsUnderPolicy()
    {
        if (!_fixture.IsAvailable)
        {
            _output.WriteLine("Skipping: Neo4j container unavailable (set ASHLAR_RUN_NEO4J_CONTAINER=1 with Docker).");
            return;
        }

        var store = new Neo4jProvenanceGraphStore(_fixture.Driver!);
        await store.ClearAsync();
        await store.EnsureSchemaAsync();

        var keys = WitnessCertificateBuilder.CreateKeyPair();
        var assetBytes = "neo4j-integration-asset"u8.ToArray();
        var bundle = WitnessCertificateBuilder.CreateBundle(
            assetBytes,
            keys.PrivateKey,
            keys.PublicKey,
            o =>
            {
                o.PolicyName = "SelfProducedBrickCertificationPolicy";
                o.PolicyVersion = "1.0.0";
            });

        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        var report = await projector.ProjectAsync([bundle]);
        report.AcceptedCount.Should().Be(1);

        var authority = new InMemoryProvenanceChainHeadAuthority(report.ChainHeadHash);
        var queries = new Neo4jProvenanceGraphQueries(_fixture.Driver!, authority);
        var result = await queries.ArtifactsUnderPolicyAsync(
            "SelfProducedBrickCertificationPolicy",
            "1.0.0");

        result.ArtifactIds.Should().Contain(bundle.ArtifactId);
        result.ChainHeadHash.Should().Be(report.ChainHeadHash);
    }

    [Fact]
    public async Task Neo4j_RejectsInvalidSignature_NothingPersisted()
    {
        if (!_fixture.IsAvailable)
            return;

        var store = new Neo4jProvenanceGraphStore(_fixture.Driver!);
        await store.ClearAsync();
        await store.EnsureSchemaAsync();

        var keys = WitnessCertificateBuilder.CreateKeyPair();
        var bundle = WitnessCertificateBuilder.CreateBundle("bad-sig-asset"u8.ToArray(), keys.PrivateKey, keys.PublicKey);
        var tampered = bundle with
        {
            Certificate = bundle.Certificate with { IssuerSignature = Convert.ToBase64String(new byte[64]) }
        };

        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        var report = await projector.ProjectAsync([tampered]);
        report.AcceptedCount.Should().Be(0);

        await using var session = _fixture.Driver!.AsyncSession();
        var cursor = await session.RunAsync("MATCH (n) RETURN count(n) AS c");
        var record = await cursor.SingleAsync();
        var count = record["c"];
        Convert.ToInt64(count).Should().Be(0);
    }

    [Fact]
    public async Task Neo4j_BatchTransaction_RollsBackOnUnknownRelationshipTarget()
    {
        if (!_fixture.IsAvailable)
            return;

        var store = new Neo4jProvenanceGraphStore(_fixture.Driver!);
        await store.ClearAsync();
        await store.EnsureSchemaAsync();
        var record = new VerifiedProvenanceRecord
        {
            ArtifactId = new string('1', 64),
            ArtifactKind = ArtifactKind.Composition,
            CertificateHash = new string('2', 64),
            SignerKeyId = "integration-signer",
            DependsOnArtifactIds = [new string('3', 64)]
        };
        var metadata = new ProvenanceGraphMetadata
        {
            ChainHeadHash = record.CertificateHash,
            ProjectedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z")
        };

        var act = () => store.ProjectBatchAsync([record], metadata);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await using var session = _fixture.Driver!.AsyncSession();
        var cursor = await session.RunAsync("MATCH (n) RETURN count(n) AS c");
        var persisted = await cursor.SingleAsync();
        Convert.ToInt64(persisted["c"]).Should().Be(0);
    }

    [Fact]
    public async Task Neo4j_QueryRefusesAuthorityOwnedStaleChainHead()
    {
        if (!_fixture.IsAvailable)
            return;

        var store = new Neo4jProvenanceGraphStore(_fixture.Driver!);
        await store.ClearAsync();
        await store.EnsureSchemaAsync();
        var keys = WitnessCertificateBuilder.CreateKeyPair();
        var bundle = WitnessCertificateBuilder.CreateBundle(
            "neo4j-staleness-asset"u8.ToArray(),
            keys.PrivateKey,
            keys.PublicKey);
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        await projector.ProjectAsync([bundle]);
        var authority = new InMemoryProvenanceChainHeadAuthority("authority-has-moved");
        var queries = new Neo4jProvenanceGraphQueries(_fixture.Driver!, authority);

        var act = () => queries.LineageOfAsync(bundle.ArtifactId);

        await act.Should().ThrowAsync<ProvenanceGraphStaleException>();
    }
}
