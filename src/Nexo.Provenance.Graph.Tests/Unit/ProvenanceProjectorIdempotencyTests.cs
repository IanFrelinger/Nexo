using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Provenance.Graph.Ingestion;
using Nexo.Provenance.Graph.InMemory;
using Nexo.Provenance.Graph.Tests.Witness;
using Xunit;

namespace Nexo.Provenance.Graph.Tests.Unit;

/// <summary>Idempotency: projecting twice yields identical node/edge counts.</summary>
public sealed class ProvenanceProjectorIdempotencyTests
{
    private static readonly byte[] WitnessAssetBytes = "idempotency-asset"u8.ToArray();
    private static readonly (byte[] PrivateKey, byte[] PublicKey) Keys = WitnessCertificateBuilder.CreateKeyPair();

    [Fact]
    public async Task ProjectTwice_NodeAndEdgeCountsUnchanged()
    {
        var store = new InMemoryProvenanceGraphStore();
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);

        var bundle = WitnessCertificateBuilder.CreateBundle(
            WitnessAssetBytes,
            Keys.PrivateKey,
            Keys.PublicKey,
            o =>
            {
                o.PolicyName = "AgentPolicyNarrowingValidator";
                o.PolicyVersion = "2.1.0";
            });

        var bundles = new[] { bundle };
        await projector.ProjectAsync(bundles);
        var first = await store.GetSnapshotAsync();

        await projector.ProjectAsync(bundles);
        var second = await store.GetSnapshotAsync();

        second!.Artifacts.Count.Should().Be(first!.Artifacts.Count);
        second.Certificates.Count.Should().Be(first.Certificates.Count);
        second.PolicyVersions.Count.Should().Be(first.PolicyVersions.Count);
        second.Edges.Count.Should().Be(first.Edges.Count);
    }
}
