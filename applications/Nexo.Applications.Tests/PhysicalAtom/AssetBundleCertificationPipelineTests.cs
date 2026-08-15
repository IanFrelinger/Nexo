using FluentAssertions;
using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;
using Nexo.Certification.Physical.Issuing;
using NSec.Cryptography;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Phase 1 asset bundle certification pipeline tests (separate witness keys from resolution tests).
/// </summary>
[Trait("Category", "Certification")]
public sealed class AssetBundleCertificationPipelineTests
{
    private static readonly (byte[] PrivateKey, byte[] PublicKey) PipelineKeys = CreatePipelineKeyPair();

    private readonly AssetBundleCertificationPipeline _pipeline =
        new(new BundleCertificationBrick(PipelineKeys.PrivateKey));

    [Fact]
    public void CertifyAndRegister_ProducesResolvableBundle()
    {
        var assetBytes = "pipeline-twin-asset-v1"u8.ToArray();
        var store = new InMemoryAssetResolutionStore();

        var result = _pipeline.CertifyAndRegister(
            store,
            new AssetBundleCertificationRequest
            {
                AtomId = Guid.NewGuid(),
                BindingScope = BindingScope.Design,
                AssetBytes = assetBytes,
                AssetVersion = "3.0.0",
                ContentType = "model/gltf-binary"
            },
            PipelineKeys.PublicKey);

        result.Succeeded.Should().BeTrue();
        result.Bundle.Should().NotBeNull();

        var resolved = PhysicalAtomResolutionVerifier.Verify(
            result.Bundle!.Certificate,
            store,
            PipelineKeys.PublicKey);

        resolved.Trusted.Should().BeTrue();

        var bundleCheck = PhysicalAtomCertBundleVerifier.Verify(result.Bundle, PipelineKeys.PublicKey);
        bundleCheck.Trusted.Should().BeTrue();
    }

    [Fact]
    public void CertifyAndRegister_RefusesInvalidBindingScope()
    {
        var store = new InMemoryAssetResolutionStore();

        var result = _pipeline.CertifyAndRegister(
            store,
            new AssetBundleCertificationRequest
            {
                AtomId = Guid.NewGuid(),
                BindingScope = BindingScope.Instance,
                AssetBytes = "x"u8.ToArray(),
                AssetVersion = "1.0.0",
                ManufactureMeta = null
            },
            PipelineKeys.PublicKey);

        result.Succeeded.Should().BeFalse();
        result.FailureCode.Should().Be("binding-scope-manufacture-meta-required");
    }

    private static (byte[] PrivateKey, byte[] PublicKey) CreatePipelineKeyPair()
    {
        using var key = Key.Create(
            SignatureAlgorithm.Ed25519,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

        return (
            key.Export(KeyBlobFormat.RawPrivateKey),
            key.PublicKey.Export(KeyBlobFormat.RawPublicKey));
    }
}
