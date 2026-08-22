using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Provenance.Graph.Ingestion;
using Ashlar.Provenance.Graph.InMemory;
using Ashlar.Provenance.Graph.Ports;
using Ashlar.Provenance.Graph.Tests.Witness;
using Xunit;

namespace Ashlar.Provenance.Graph.Tests.Unit;

/// <summary>Query layer fail-closed staleness tests.</summary>
public sealed class ProvenanceGraphQueryStalenessTests
{
    private static readonly byte[] WitnessAssetBytes = "staleness-test-asset"u8.ToArray();
    private static readonly (byte[] PrivateKey, byte[] PublicKey) Keys = WitnessCertificateBuilder.CreateKeyPair();

    [Fact]
    public async Task LineageOf_RefusesStaleGraph_WithFailClosedError()
    {
        var store = new InMemoryProvenanceGraphStore();
        var authority = new InMemoryProvenanceChainHeadAuthority("stale-chain-head-hash");
        var queries = new InMemoryProvenanceGraphQueries(store, authority);
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);

        var bundle = WitnessCertificateBuilder.CreateBundle(WitnessAssetBytes, Keys.PrivateKey, Keys.PublicKey);
        var report = await projector.ProjectAsync([bundle]);

        var act = () => queries.LineageOfAsync(bundle.ArtifactId);

        await act.Should().ThrowAsync<ProvenanceGraphStaleException>()
            .Where(ex => ex.CurrentChainHead == "stale-chain-head-hash");
    }

    [Fact]
    public async Task ArtifactsUnderPolicy_RefusesStaleGraph_WithFailClosedError()
    {
        var store = new InMemoryProvenanceGraphStore();
        var authority = new InMemoryProvenanceChainHeadAuthority("wrong-head");
        var queries = new InMemoryProvenanceGraphQueries(store, authority);
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
            "1.0.0");

        await act.Should().ThrowAsync<ProvenanceGraphStaleException>();
    }

    [Fact]
    public async Task BlastRadius_RefusesStaleGraph_FromAuthority()
    {
        var store = new InMemoryProvenanceGraphStore();
        var authority = new InMemoryProvenanceChainHeadAuthority("authority-moved");
        var queries = new InMemoryProvenanceGraphQueries(store, authority);
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        var bundle = WitnessCertificateBuilder.CreateBundle(
            WitnessAssetBytes,
            Keys.PrivateKey,
            Keys.PublicKey);
        await projector.ProjectAsync([bundle]);

        var act = () => queries.BlastRadiusOfAsync("policy", "1.0.0");

        await act.Should().ThrowAsync<ProvenanceGraphStaleException>()
            .Where(ex => ex.CurrentChainHead == "authority-moved");
    }
}
