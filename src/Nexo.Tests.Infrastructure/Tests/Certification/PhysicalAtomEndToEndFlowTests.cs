using FluentAssertions;
using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;
using Nexo.Certification.Physical.Resolution.Http;
using Nexo.Certification.Physical.Tagging;
using Nexo.Infrastructure.Certification.Physical;
using NSec.Cryptography;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Phase 3 end-to-end flow: pipeline → HTTP store → tag → orchestrated verify.
/// </summary>
[Trait("Category", "Certification")]
public sealed class PhysicalAtomEndToEndFlowTests
{
    private static readonly (byte[] PrivateKey, byte[] PublicKey) Keys = CreateKeys();

    [Fact]
    public void Certify_Register_HttpResolve_TagVerify_Accepts()
    {
        var store = new InMemoryAssetResolutionStore();
        var pipeline = new AssetBundleCertificationPipeline(new BundleCertificationBrick(Keys.PrivateKey));
        var assetBytes = "e2e-flow-asset-v1"u8.ToArray();
        var atomId = Guid.NewGuid();

        var certified = pipeline.CertifyAndRegister(
            store,
            new AssetBundleCertificationRequest
            {
                AtomId = atomId,
                BindingScope = BindingScope.Design,
                AssetBytes = assetBytes,
                AssetVersion = "4.0.0"
            },
            Keys.PublicKey);

        certified.Succeeded.Should().BeTrue();

        var tag = PhysicalAtomTagIssuingBrick.IssueFromBundle(
            certified.Bundle!,
            TagReferenceKind.BundleRef);

        tag.Succeeded.Should().BeTrue();

        var certResponse = HttpAssetResolutionRouter.Handle(
            "GET",
            $"/nexo/atoms/{atomId}/cert",
            store);
        certResponse.StatusCode.Should().Be(200);

        var assetHash = certified.Bundle!.Certificate.AssetHash;
        var assetResponse = HttpAssetResolutionRouter.Handle(
            "GET",
            $"/nexo/assets/{assetHash}/4.0.0",
            store);
        assetResponse.StatusCode.Should().Be(200);
        assetResponse.Body.Should().Equal(assetBytes);

        var verify = PhysicalAtomTagVerifyOrchestrator.VerifyQr(
            tag.QrPayload!,
            store,
            Keys.PublicKey);

        verify.Trusted.Should().BeTrue();
    }

    private static (byte[] PrivateKey, byte[] PublicKey) CreateKeys()
    {
        using var key = Key.Create(
            SignatureAlgorithm.Ed25519,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

        return (
            key.Export(KeyBlobFormat.RawPrivateKey),
            key.PublicKey.Export(KeyBlobFormat.RawPublicKey));
    }
}
