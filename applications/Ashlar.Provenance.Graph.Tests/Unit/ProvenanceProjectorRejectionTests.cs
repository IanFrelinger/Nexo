using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Certification.Physical;
using Ashlar.Provenance.Graph.Hashing;
using Ashlar.Provenance.Graph.Ingestion;
using Ashlar.Provenance.Graph.InMemory;
using Ashlar.Provenance.Graph.Models;
using Ashlar.Provenance.Graph.Tests.Witness;
using Xunit;

namespace Ashlar.Provenance.Graph.Tests.Unit;

/// <summary>
/// Ingestion rejection tests — prove verification gates have teeth before projection.
/// </summary>
public sealed class ProvenanceProjectorRejectionTests
{
  private static readonly byte[] WitnessAssetBytes = "witness-provenance-asset-v1"u8.ToArray();
    private static readonly (byte[] PrivateKey, byte[] PublicKey) Keys = WitnessCertificateBuilder.CreateKeyPair();

    [Fact]
    public async Task Rejects_InvalidSignature_AndReports_NothingWritten()
    {
        var store = new InMemoryProvenanceGraphStore();
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        var bundle = WitnessCertificateBuilder.CreateBundle(WitnessAssetBytes, Keys.PrivateKey, Keys.PublicKey);

        var tampered = bundle with
        {
            Certificate = bundle.Certificate with
            {
                IssuerSignature = Convert.ToBase64String(new byte[64])
            }
        };

        var report = await projector.ProjectAsync([tampered]);

        report.AcceptedCount.Should().Be(0);
        report.Rejections.Should().ContainSingle();
        report.Rejections[0].FailureCode.Should().Be("signature-invalid");

        var snapshot = await store.GetSnapshotAsync();
        snapshot!.Artifacts.Should().BeEmpty();
        snapshot.Certificates.Should().BeEmpty();
        snapshot.Edges.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_ContentHashMismatch_AndReports_NothingWritten()
    {
        var store = new InMemoryProvenanceGraphStore();
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        var bundle = WitnessCertificateBuilder.CreateBundle(WitnessAssetBytes, Keys.PrivateKey, Keys.PublicKey);

        var wrongContent = "different-bytes"u8.ToArray();
        var mismatched = bundle with { ArtifactContent = wrongContent };

        var report = await projector.ProjectAsync([mismatched]);

        report.AcceptedCount.Should().Be(0);
        report.Rejections.Should().ContainSingle();
        report.Rejections[0].FailureCode.Should().BeOneOf("asset-hash-mismatch", "content-hash-mismatch", "artifact-id-mismatch");

        var snapshot = await store.GetSnapshotAsync();
        snapshot!.Edges.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_CertificateAssetHashMismatch_AndReports_NothingWritten()
    {
        var store = new InMemoryProvenanceGraphStore();
        var projector = new ProvenanceProjector(store, NullLogger<ProvenanceProjector>.Instance);
        var bundle = WitnessCertificateBuilder.CreateBundle(WitnessAssetBytes, Keys.PrivateKey, Keys.PublicKey);

        var wrongHash = AssetContentHasher.ComputeSha256Hex("other-content"u8.ToArray());
        var unsigned = bundle.Certificate with { AssetHash = wrongHash };
        var mismatchedCert = WitnessCertificateBuilder.Sign(unsigned, Keys.PrivateKey);
        var mismatched = bundle with { Certificate = mismatchedCert };

        var report = await projector.ProjectAsync([mismatched]);

        report.AcceptedCount.Should().Be(0);
        report.Rejections.Should().ContainSingle();
        report.Rejections[0].FailureCode.Should().Be("asset-hash-mismatch");

        var snapshot = await store.GetSnapshotAsync();
        snapshot!.Edges.Should().BeEmpty();
    }
}
