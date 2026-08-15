using FluentAssertions;
using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;
using Nexo.Certification.Physical.Tagging;
using Nexo.Certification.Physical.Issuing;
using NSec.Cryptography;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Phase 2 tag issuance from certified bundles (separate from codec witness fixtures).
/// </summary>
[Trait("Category", "Certification")]
public sealed class PhysicalAtomTagIssuingTests
{
    private static readonly (byte[] PrivateKey, byte[] PublicKey) IssuerKeys = CreateIssuerKeyPair();

    private readonly AssetBundleCertificationPipeline _pipeline =
        new(new BundleCertificationBrick(IssuerKeys.PrivateKey));

    [Fact]
    public void IssueFromBundle_ProducesQrAndNfcTags()
    {
        var store = new InMemoryAssetResolutionStore();
        var assetBytes = "tag-issue-asset-v1"u8.ToArray();
        var atomId = Guid.NewGuid();

        var certified = _pipeline.CertifyAndRegister(
            store,
            new AssetBundleCertificationRequest
            {
                AtomId = atomId,
                BindingScope = BindingScope.Design,
                AssetBytes = assetBytes,
                AssetVersion = "2.0.0"
            },
            IssuerKeys.PublicKey);

        certified.Succeeded.Should().BeTrue();

        var tag = PhysicalAtomTagIssuingBrick.IssueFromBundle(
            certified.Bundle!,
            TagReferenceKind.BundleRef);

        tag.Succeeded.Should().BeTrue();
        tag.Reference!.AtomId.Should().Be(atomId);

        PhysicalAtomQrTagCodec.TryDecode(tag.QrPayload!, out var fromQr, out _, out _).Should().BeTrue();
        fromQr!.AtomId.Should().Be(atomId);

        PhysicalAtomNfcNdefCodec.TryDecode(tag.NdefRecord!, out var ndefPayload, out _, out _).Should().BeTrue();
        PhysicalAtomTagBinaryCodec.TryDecode(ndefPayload, out var fromNfc, out _, out _).Should().BeTrue();
        fromNfc!.AtomId.Should().Be(atomId);
    }

    [Fact]
    public void IssueFromCert_RefusesWhenIssuerFingerprintMissing()
    {
        var cert = new PhysicalAtomCertificate
        {
            AtomId = Guid.NewGuid(),
            BindingScope = BindingScope.Design,
            AssetHash = AssetContentHasher.ComputeSha256Hex("x"u8.ToArray()),
            AssetVersion = "1.0.0"
        };

        var tag = PhysicalAtomTagIssuingBrick.IssueFromCert(cert, TagReferenceKind.CertRef, issuerPublicKey: null);

        tag.Succeeded.Should().BeFalse();
        tag.FailureCode.Should().Be("issuer-public-key-missing");
    }

    private static (byte[] PrivateKey, byte[] PublicKey) CreateIssuerKeyPair()
    {
        using var key = Key.Create(
            SignatureAlgorithm.Ed25519,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

        return (
            key.Export(KeyBlobFormat.RawPrivateKey),
            key.PublicKey.Export(KeyBlobFormat.RawPublicKey));
    }
}
