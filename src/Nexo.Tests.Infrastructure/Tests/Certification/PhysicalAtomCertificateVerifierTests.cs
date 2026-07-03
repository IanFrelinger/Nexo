using FluentAssertions;
using Nexo.Certification.Physical;
using NSec.Cryptography;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Physical-atom certificate verifier refusal/admission tests.
/// Witness fixtures are hand-authored independently of the issuance code path.
/// </summary>
[Trait("Category", "Certification")]
public sealed class PhysicalAtomCertificateVerifierTests
{
    private static readonly SignatureAlgorithm Algorithm = SignatureAlgorithm.Ed25519;

    private static readonly byte[] WitnessAssetBytes =
        "witness-digital-twin-asset-v1"u8.ToArray();

    private static readonly string WitnessAssetHash =
        AssetContentHasher.ComputeSha256Hex(WitnessAssetBytes);

    private static readonly (byte[] PrivateKey, byte[] PublicKey) WitnessIssuerKeys = CreateWitnessKeyPair();

    private static readonly (byte[] PrivateKey, byte[] PublicKey) ForgedIssuerKeys = CreateWitnessKeyPair();

    [Fact]
    public void R1_ForgedSignature_Refuses()
    {
        var cert = WitnessBuilder.CreateSigned(
            BindingScope.Design,
            WitnessAssetHash,
            WitnessIssuerKeys.PrivateKey);

        var forged = cert with { IssuerSignature = Convert.ToBase64String(new byte[64]) };

        var result = PhysicalAtomCertificateVerifier.Verify(
            forged,
            WitnessAssetBytes,
            WitnessIssuerKeys.PublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("signature-invalid");
    }

    [Fact]
    public void R2_AssetHashMismatch_Refuses()
    {
        var cert = WitnessBuilder.CreateSigned(
            BindingScope.Design,
            WitnessAssetHash,
            WitnessIssuerKeys.PrivateKey);

        var result = PhysicalAtomCertificateVerifier.Verify(
            cert,
            "different-asset-bytes"u8.ToArray(),
            WitnessIssuerKeys.PublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("asset-hash-mismatch");
    }

    [Fact]
    public void R3_InstanceScopeMissingManufactureMeta_Refuses()
    {
        var unsigned = WitnessBuilder.CreateUnsigned(
            BindingScope.Instance,
            WitnessAssetHash,
            manufactureMeta: null);

        var cert = WitnessBuilder.Sign(unsigned, WitnessIssuerKeys.PrivateKey);

        var result = PhysicalAtomCertificateVerifier.Verify(
            cert,
            WitnessAssetBytes,
            WitnessIssuerKeys.PublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("binding-scope-manufacture-meta-required");
    }

    [Fact]
    public void R4_DesignScopeWithManufactureMeta_Refuses()
    {
        var unsigned = WitnessBuilder.CreateUnsigned(
            BindingScope.Design,
            WitnessAssetHash,
            manufactureMeta: new ManufactureMeta("BATCH-001", "SN-001", DateTimeOffset.Parse("2026-01-01T00:00:00Z")));

        var cert = WitnessBuilder.Sign(unsigned, WitnessIssuerKeys.PrivateKey);

        var result = PhysicalAtomCertificateVerifier.Verify(
            cert,
            WitnessAssetBytes,
            WitnessIssuerKeys.PublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("binding-scope-manufacture-meta-forbidden");
    }

    [Fact]
    public void R5_GeoAnchorH3IndexInconsistent_Refuses()
    {
        var geo = new GeoAnchor(37.7749, -122.4194, 9, "000000000000000");
        var unsigned = WitnessBuilder.CreateUnsigned(
            BindingScope.Design,
            WitnessAssetHash,
            geoAnchor: geo);

        var cert = WitnessBuilder.Sign(unsigned, WitnessIssuerKeys.PrivateKey);

        var result = PhysicalAtomCertificateVerifier.Verify(
            cert,
            WitnessAssetBytes,
            WitnessIssuerKeys.PublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("geo-anchor-inconsistent");
    }

    [Fact]
    public void R6_TamperedExtensions_Refuses()
    {
        var extensions = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["vendor.note"] = "original"u8.ToArray()
        };

        var unsigned = WitnessBuilder.CreateUnsigned(
            BindingScope.Design,
            WitnessAssetHash,
            extensions: extensions);

        var cert = WitnessBuilder.Sign(unsigned, WitnessIssuerKeys.PrivateKey);
        var tamperedExtensions = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["vendor.note"] = "tampered"u8.ToArray()
        };
        var tampered = cert with { Extensions = tamperedExtensions };

        var result = PhysicalAtomCertificateVerifier.Verify(
            tampered,
            WitnessAssetBytes,
            WitnessIssuerKeys.PublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("signature-invalid");
    }

    [Fact]
    public void R7_WrongIssuerPublicKey_Refuses()
    {
        var cert = WitnessBuilder.CreateSigned(
            BindingScope.Design,
            WitnessAssetHash,
            WitnessIssuerKeys.PrivateKey);

        var result = PhysicalAtomCertificateVerifier.Verify(
            cert,
            WitnessAssetBytes,
            ForgedIssuerKeys.PublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("signature-invalid");
    }

    [Fact]
    public void A1_DesignScopeWellFormed_Accepts()
    {
        var cert = WitnessBuilder.CreateSigned(
            BindingScope.Design,
            WitnessAssetHash,
            WitnessIssuerKeys.PrivateKey);

        var result = PhysicalAtomCertificateVerifier.Verify(
            cert,
            WitnessAssetBytes,
            WitnessIssuerKeys.PublicKey);

        result.Trusted.Should().BeTrue();
        result.FailureCode.Should().BeNull();
    }

    [Fact]
    public void A2_InstanceScopeWellFormed_Accepts()
    {
        var unsigned = WitnessBuilder.CreateUnsigned(
            BindingScope.Instance,
            WitnessAssetHash,
            manufactureMeta: new ManufactureMeta(null, "SN-WITNESS-001", DateTimeOffset.Parse("2026-03-15T12:00:00Z")));

        var cert = WitnessBuilder.Sign(unsigned, WitnessIssuerKeys.PrivateKey);

        var result = PhysicalAtomCertificateVerifier.Verify(
            cert,
            WitnessAssetBytes,
            WitnessIssuerKeys.PublicKey);

        result.Trusted.Should().BeTrue();
    }

    [Fact]
    public void A3_BatchScopeWellFormed_Accepts()
    {
        var unsigned = WitnessBuilder.CreateUnsigned(
            BindingScope.Batch,
            WitnessAssetHash,
            manufactureMeta: new ManufactureMeta("BATCH-WITNESS-42", null, DateTimeOffset.Parse("2026-02-01T08:30:00Z")));

        var cert = WitnessBuilder.Sign(unsigned, WitnessIssuerKeys.PrivateKey);

        var result = PhysicalAtomCertificateVerifier.Verify(
            cert,
            WitnessAssetBytes,
            WitnessIssuerKeys.PublicKey);

        result.Trusted.Should().BeTrue();
    }

    [Fact]
    public void A4_DesignScopeWithConsistentGeoAnchor_Accepts()
    {
        var geo = new GeoAnchor(37.7749, -122.4194, 9, "897016d01d3ffff");
        var unsigned = WitnessBuilder.CreateUnsigned(
            BindingScope.Design,
            WitnessAssetHash,
            geoAnchor: geo);

        var cert = WitnessBuilder.Sign(unsigned, WitnessIssuerKeys.PrivateKey);

        var result = PhysicalAtomCertificateVerifier.Verify(
            cert,
            WitnessAssetBytes,
            WitnessIssuerKeys.PublicKey);

        result.Trusted.Should().BeTrue();
    }

    private static (byte[] PrivateKey, byte[] PublicKey) CreateWitnessKeyPair()
    {
        using var key = Key.Create(
            Algorithm,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });

        return (
            key.Export(KeyBlobFormat.RawPrivateKey),
            key.PublicKey.Export(KeyBlobFormat.RawPublicKey));
    }

    /// <summary>
    /// Independent witness oracle — constructs and signs certs without the issuance brick.
    /// </summary>
    private static class WitnessBuilder
    {
        public static PhysicalAtomCertificate CreateUnsigned(
            BindingScope scope,
            string assetHash,
            GeoAnchor? geoAnchor = null,
            ManufactureMeta? manufactureMeta = null,
            IReadOnlyDictionary<string, byte[]>? extensions = null) =>
            new()
            {
                AtomId = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8"),
                BindingScope = scope,
                AssetHash = assetHash,
                AssetVersion = "1.0.0",
                GeoAnchor = geoAnchor,
                ManufactureMeta = manufactureMeta,
                Extensions = extensions ?? new Dictionary<string, byte[]>(StringComparer.Ordinal)
            };

        public static PhysicalAtomCertificate Sign(PhysicalAtomCertificate unsigned, byte[] privateKey) =>
            unsigned with
            {
                IssuerSignature = PhysicalAtomCertificateSigning.Sign(unsigned, privateKey)
            };

        public static PhysicalAtomCertificate CreateSigned(
            BindingScope scope,
            string assetHash,
            byte[] privateKey) =>
            Sign(CreateUnsigned(scope, assetHash), privateKey);
    }
}
