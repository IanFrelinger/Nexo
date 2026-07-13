using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Provenance.Graph.Ingestion;
using Nexo.Provenance.Graph.InMemory;
using Nexo.Provenance.Graph.Tests.Witness;
using Xunit;

namespace Nexo.Provenance.Graph.Tests.Unit;

/// <summary>Query layer fail-closed staleness tests.</summary>
public sealed class ProvenanceGraphQueryStalenessTests
{
    private static readonly byte[] WitnessAssetBytes = "staleness-test-asset"u8.ToArray();
    private static readonly (byte[] PrivateKey, byte[] PublicKey) Keys = WitnessCertificateBuilder.CreateKeyPair();

    [Fact]
    public async Task LineageOf_RefusesStaleGraph_WithFailClosedError()
    {
        var store = new InMemoryProvenanceGraphStore();
        var queries = new InMemoryProvenanceGraphQueries(store);
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);

        var bundle = WitnessCertificateBuilder.CreateBundle(WitnessAssetBytes, Keys.PrivateKey, Keys.PublicKey);
        var report = await projector.ProjectAsync([bundle]);

        var act = () => queries.LineageOfAsync(bundle.ArtifactId, "stale-chain-head-hash");

        await act.Should().ThrowAsync<ProvenanceGraphStaleException>()
            .Where(ex => ex.CurrentChainHead == "stale-chain-head-hash");
    }

    [Fact]
    public async Task ArtifactsUnderPolicy_RefusesStaleGraph_WithFailClosedError()
    {
        var store = new InMemoryProvenanceGraphStore();
        var queries = new InMemoryProvenanceGraphQueries(store);
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);

        var bundle = WitnessCertificateBuilder.CreateBundle(
            WitnessAssetBytes,
            Keys.PrivateKey,
            Keys.PublicKey,
            o =>
            {
                o.PolicyName = "SelfProducedBrickCertificationPolicy";
                o.PolicyVersion = "1.0.0";
            });

        await projector.ProjectAsync([bundle]);

        var act = () => queries.ArtifactsUnderPolicyAsync(
            "SelfProducedBrickCertificationPolicy",
            "1.0.0",
            "wrong-head");

        await act.Should().ThrowAsync<ProvenanceGraphStaleException>();
    }
}
