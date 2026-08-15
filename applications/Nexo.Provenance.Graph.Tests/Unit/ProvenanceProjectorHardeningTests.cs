using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Provenance.Graph.Ingestion;
using Nexo.Provenance.Graph.InMemory;
using Nexo.Provenance.Graph.Models;
using Nexo.Provenance.Graph.Tests.Witness;
using Xunit;

namespace Nexo.Provenance.Graph.Tests.Unit;

public sealed class ProvenanceProjectorHardeningTests
{
    private static readonly (byte[] PrivateKey, byte[] PublicKey) Keys =
        WitnessCertificateBuilder.CreateKeyPair();

    [Fact]
    public async Task Rejects_AmbiguousIndependentChains_WithoutWritingAnything()
    {
        var store = new InMemoryProvenanceGraphStore();
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        var first = WitnessCertificateBuilder.CreateBundle(
            "independent-a"u8.ToArray(),
            Keys.PrivateKey,
            Keys.PublicKey);
        var second = WitnessCertificateBuilder.CreateBundle(
            "independent-b"u8.ToArray(),
            Keys.PrivateKey,
            Keys.PublicKey);

        var report = await projector.ProjectAsync([first, second]);

        report.AcceptedCount.Should().Be(0);
        report.Rejections.Should().ContainSingle(r => r.FailureCode == "chain-head-ambiguous");
        var snapshot = await store.GetSnapshotAsync();
        snapshot!.Artifacts.Should().BeEmpty();
        snapshot.Certificates.Should().BeEmpty();
        snapshot.Edges.Should().BeEmpty();
        snapshot.Metadata.Should().BeNull();
    }

    [Fact]
    public async Task Rejects_UnknownPriorCertificate_WithoutCreatingPhantomNode()
    {
        var store = new InMemoryProvenanceGraphStore();
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        var bundle = WitnessCertificateBuilder.CreateBundle(
            "unknown-prior"u8.ToArray(),
            Keys.PrivateKey,
            Keys.PublicKey,
            o => o.PriorCertificateHash = new string('a', 64));

        var report = await projector.ProjectAsync([bundle]);

        report.AcceptedCount.Should().Be(0);
        report.Rejections.Should().ContainSingle(r => r.FailureCode == "chain-reference-missing");
        var snapshot = await store.GetSnapshotAsync();
        snapshot!.Certificates.Should().BeEmpty();
        snapshot.Edges.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_UnknownDependency_WithoutCreatingPhantomNode()
    {
        var store = new InMemoryProvenanceGraphStore();
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        var bundle = WitnessCertificateBuilder.CreateBundle(
            "unknown-dependency"u8.ToArray(),
            Keys.PrivateKey,
            Keys.PublicKey,
            o => o.DependsOnArtifactIds = [new string('b', 64)]);

        var report = await projector.ProjectAsync([bundle]);

        report.AcceptedCount.Should().Be(0);
        report.Rejections.Should().ContainSingle(r => r.FailureCode == "dependency-reference-missing");
        var snapshot = await store.GetSnapshotAsync();
        snapshot!.Artifacts.Should().BeEmpty();
        snapshot.Edges.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_MalformedSignedProvenanceClaims()
    {
        var store = new InMemoryProvenanceGraphStore();
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        var bundle = WitnessCertificateBuilder.CreateBundle(
            "malformed-claims"u8.ToArray(),
            Keys.PrivateKey,
            Keys.PublicKey);
        var extensions = new Dictionary<string, byte[]>(bundle.Certificate.Extensions, StringComparer.Ordinal)
        {
            ["nexo.provenance.v1"] = "{not-json"u8.ToArray()
        };
        var unsigned = bundle.Certificate with { Extensions = extensions, IssuerSignature = null };
        var signed = WitnessCertificateBuilder.Sign(unsigned, Keys.PrivateKey);

        var report = await projector.ProjectAsync([bundle with { Certificate = signed }]);

        report.AcceptedCount.Should().Be(0);
        report.Rejections.Should().ContainSingle(r => r.FailureCode == "provenance-claims-invalid");
        var snapshot = await store.GetSnapshotAsync();
        snapshot!.Edges.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_UnsignedBundleOverlay_EvenWhenCertificateIsValid()
    {
        var store = new InMemoryProvenanceGraphStore();
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        var bundle = WitnessCertificateBuilder.CreateBundle(
            "unsigned-overlay"u8.ToArray(),
            Keys.PrivateKey,
            Keys.PublicKey);

        var report = await projector.ProjectAsync(
            [bundle with { PolicyName = "ForgedPolicy", PolicyVersion = "9.9.9" }]);

        report.AcceptedCount.Should().Be(0);
        report.Rejections.Should().ContainSingle(r => r.FailureCode == "unsigned-provenance-overlay");
        var snapshot = await store.GetSnapshotAsync();
        snapshot!.Edges.Should().BeEmpty();
    }

    [Fact]
    public async Task InMemoryBatch_RollsBackAllNodes_WhenRelationshipValidationFails()
    {
        var store = new InMemoryProvenanceGraphStore();
        var record = new VerifiedProvenanceRecord
        {
            ArtifactId = new string('1', 64),
            ArtifactKind = ArtifactKind.Composition,
            CertificateHash = new string('2', 64),
            SignerKeyId = "signer",
            DependsOnArtifactIds = [new string('3', 64)]
        };
        var metadata = new ProvenanceGraphMetadata
        {
            ChainHeadHash = record.CertificateHash,
            ProjectedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z")
        };

        var act = () => store.ProjectBatchAsync([record], metadata);

        await act.Should().ThrowAsync<InvalidOperationException>();
        var snapshot = await store.GetSnapshotAsync();
        snapshot!.Artifacts.Should().BeEmpty();
        snapshot.Certificates.Should().BeEmpty();
        snapshot.Edges.Should().BeEmpty();
        snapshot.Metadata.Should().BeNull();
    }
}
