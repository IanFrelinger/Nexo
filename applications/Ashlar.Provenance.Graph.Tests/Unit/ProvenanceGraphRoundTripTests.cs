using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Provenance.Graph.Ingestion;
using Ashlar.Provenance.Graph.InMemory;
using Ashlar.Provenance.Graph.Tests.Witness;
using Xunit;

namespace Ashlar.Provenance.Graph.Tests.Unit;

/// <summary>
/// Round-trip verification: every projected edge must be re-derivable from cert bytes via witness oracle.
/// </summary>
public sealed class ProvenanceGraphRoundTripTests
{
    private static readonly byte[] AssetA = "roundtrip-asset-a"u8.ToArray();
    private static readonly byte[] AssetB = "roundtrip-asset-b"u8.ToArray();
    private static readonly (byte[] PrivateKey, byte[] PublicKey) Keys = WitnessCertificateBuilder.CreateKeyPair();

    [Fact]
    public async Task EveryProjectedEdge_IsReDerivableFromCertificatePayloads()
    {
        var store = new InMemoryProvenanceGraphStore();
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);

        var bundleA = WitnessCertificateBuilder.CreateBundle(
            AssetA,
            Keys.PrivateKey,
            Keys.PublicKey,
            o =>
            {
                o.PolicyName = "SelfProducedBrickCertificationPolicy";
                o.PolicyVersion = "1.0.0";
                o.ProducerAgentId = "agent-witness-001";
                o.ProducerAgentKind = Models.AgentKind.Agent;
                o.IssuedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
            });

        var bundleB = WitnessCertificateBuilder.CreateBundle(
            AssetB,
            Keys.PrivateKey,
            Keys.PublicKey,
            o =>
            {
                o.ArtifactKind = Models.ArtifactKind.Composition;
                o.DependsOnArtifactIds = [bundleA.ArtifactId];
                o.PriorCertificateHash = Ashlar.Provenance.Graph.Hashing.ProvenanceCertificateHasher.ComputeCertificateHash(bundleA.Certificate);
                o.IssuedAt = DateTimeOffset.Parse("2026-02-01T00:00:00Z");
            });

        await projector.ProjectAsync([bundleA, bundleB]);

        var snapshot = await store.GetSnapshotAsync();
        snapshot.Should().NotBeNull();

        var expectedEdges = WitnessGraphOracle.DeriveEdges(bundleA)
            .Concat(WitnessGraphOracle.DeriveEdges(bundleB))
            .ToList();

        WitnessGraphOracle.EdgesMatchSnapshot(expectedEdges, snapshot!.Edges).Should().BeTrue(
            "every edge in the projected graph must match witness-derived edges from cert payloads");
    }
}
