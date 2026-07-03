using FluentAssertions;
using Nexo.Certification.Physical;
using Nexo.Infrastructure.Certification.Physical;
using NSec.Cryptography;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// BundleCertificationBrick issuance tests (separate from verifier witness fixtures).
/// </summary>
[Trait("Category", "Certification")]
public sealed class BundleCertificationBrickTests
{
    private static readonly byte[] IssuerAssetBytes = "issuer-twin-asset-v2"u8.ToArray();

    private static readonly (byte[] PrivateKey, byte[] PublicKey) IssuerKeys = CreateIssuerKeyPair();

    private readonly BundleCertificationBrick _brick = new(IssuerKeys.PrivateKey);

    [Fact]
    public void Issue_DesignScope_ProducesVerifiableCertificate()
    {
        var result = _brick.Issue(new BundleCertificationRequest
        {
            AtomId = Guid.NewGuid(),
            BindingScope = BindingScope.Design,
            AssetBytes = IssuerAssetBytes,
            AssetVersion = "2.1.0"
        });

        result.Succeeded.Should().BeTrue();
        result.Certificate.Should().NotBeNull();

        var verify = PhysicalAtomCertificateVerifier.Verify(
            result.Certificate!,
            IssuerAssetBytes,
            IssuerKeys.PublicKey);

        verify.Trusted.Should().BeTrue();
        result.Certificate!.BindingScope.Should().Be(BindingScope.Design);
        result.Certificate.ManufactureMeta.Should().BeNull();
    }

    [Fact]
    public void Issue_InstanceScope_ProducesVerifiableCertificate()
    {
        var result = _brick.Issue(new BundleCertificationRequest
        {
            AtomId = Guid.NewGuid(),
            BindingScope = BindingScope.Instance,
            AssetBytes = IssuerAssetBytes,
            AssetVersion = "2.1.0",
            ManufactureMeta = new ManufactureMeta(null, "SN-ISSUER-007", DateTimeOffset.UtcNow)
        });

        result.Succeeded.Should().BeTrue();

        var verify = PhysicalAtomCertificateVerifier.Verify(
            result.Certificate!,
            IssuerAssetBytes,
            IssuerKeys.PublicKey);

        verify.Trusted.Should().BeTrue();
        result.Certificate!.ManufactureMeta!.SerialNumber.Should().Be("SN-ISSUER-007");
    }

    [Fact]
    public void Issue_BatchScope_ProducesVerifiableCertificate()
    {
        var result = _brick.Issue(new BundleCertificationRequest
        {
            AtomId = Guid.NewGuid(),
            BindingScope = BindingScope.Batch,
            AssetBytes = IssuerAssetBytes,
            AssetVersion = "2.1.0",
            ManufactureMeta = new ManufactureMeta("BATCH-ISSUER-99", null, DateTimeOffset.UtcNow)
        });

        result.Succeeded.Should().BeTrue();

        var verify = PhysicalAtomCertificateVerifier.Verify(
            result.Certificate!,
            IssuerAssetBytes,
            IssuerKeys.PublicKey);

        verify.Trusted.Should().BeTrue();
        result.Certificate!.ManufactureMeta!.BatchId.Should().Be("BATCH-ISSUER-99");
    }

    [Fact]
    public void Issue_InstanceWithoutManufactureMeta_Refuses()
    {
        var result = _brick.Issue(new BundleCertificationRequest
        {
            AtomId = Guid.NewGuid(),
            BindingScope = BindingScope.Instance,
            AssetBytes = IssuerAssetBytes,
            AssetVersion = "1.0.0",
            ManufactureMeta = null
        });

        result.Succeeded.Should().BeFalse();
        result.FailureCode.Should().Be("binding-scope-manufacture-meta-required");
    }

    [Fact]
    public void Issue_DesignWithManufactureMeta_Refuses()
    {
        var result = _brick.Issue(new BundleCertificationRequest
        {
            AtomId = Guid.NewGuid(),
            BindingScope = BindingScope.Design,
            AssetBytes = IssuerAssetBytes,
            AssetVersion = "1.0.0",
            ManufactureMeta = new ManufactureMeta("BATCH-X", "SN-X", DateTimeOffset.UtcNow)
        });

        result.Succeeded.Should().BeFalse();
        result.FailureCode.Should().Be("binding-scope-manufacture-meta-forbidden");
    }

    [Fact]
    public void Issue_InconsistentGeoAnchor_Refuses()
    {
        var result = _brick.Issue(new BundleCertificationRequest
        {
            AtomId = Guid.NewGuid(),
            BindingScope = BindingScope.Design,
            AssetBytes = IssuerAssetBytes,
            AssetVersion = "1.0.0",
            GeoAnchor = new GeoAnchor(37.7749, -122.4194, 9, "000000000000000")
        });

        result.Succeeded.Should().BeFalse();
        result.FailureCode.Should().Be("geo-anchor-inconsistent");
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
