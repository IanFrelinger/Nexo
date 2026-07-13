using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Neo4j.Driver;
using Nexo.Provenance.Graph.Ingestion;
using Nexo.Provenance.Graph.Models;
using Nexo.Provenance.Graph.Neo4j;
using Nexo.Provenance.Graph.Tests.Witness;
using Testcontainers.Neo4j;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Provenance.Graph.Tests.Integration;

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
            _output.WriteLine("Skipping: Neo4j container unavailable (set NEXO_RUN_NEO4J_CONTAINER=1 with Docker).");
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

        var queries = new Neo4jProvenanceGraphQueries(_fixture.Driver!);
        var result = await queries.ArtifactsUnderPolicyAsync(
            "SelfProducedBrickCertificationPolicy",
            "1.0.0",
            report.ChainHeadHash);

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
}
