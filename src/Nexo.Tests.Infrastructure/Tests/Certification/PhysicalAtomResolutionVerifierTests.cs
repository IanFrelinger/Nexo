using FluentAssertions;
using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Phase 1 resolution verifier refusal/admission tests.
/// Witness fixtures are independent of the certification pipeline.
/// </summary>
[Trait("Category", "Certification")]
public sealed class PhysicalAtomResolutionVerifierTests
{
    private static readonly byte[] WitnessAssetBytes = "resolution-witness-asset-v1"u8.ToArray();

    private static readonly string WitnessAssetHash =
        AssetContentHasher.ComputeSha256Hex(WitnessAssetBytes);

    private static readonly byte[] WitnessIssuerPublicKey =
        Convert.FromBase64String("SOAn5IcnCADrZ0YtsWCmD6cZacp/xtKi8/iNwPDvjU0=");

    private static readonly Guid WitnessAtomId =
        Guid.Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

    [Fact]
    public void R1_UnresolvedAtomId_Refuses()
    {
        var store = ResolutionWitness.CreateStore(withAsset: true, withCert: false);
        var cert = ResolutionWitness.CreateSignedCert(WitnessAtomId, WitnessAssetHash);

        var result = PhysicalAtomResolutionVerifier.Verify(
            cert,
            store,
            WitnessIssuerPublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("atom-unresolved");
    }

    [Fact]
    public void R2_StoreAssetBytesMismatch_Refuses()
    {
        var store = ResolutionWitness.CreateStore(withAsset: false, withCert: true);
        store.RegisterAsset(new DigitalTwinAssetRecord(
            WitnessAssetHash,
            "1.0.0",
            "application/octet-stream",
            "wrong-bytes-for-hash"u8.ToArray()));

        var cert = ResolutionWitness.CreateSignedCert(WitnessAtomId, WitnessAssetHash);

        var result = PhysicalAtomResolutionVerifier.Verify(
            cert,
            store,
            WitnessIssuerPublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("asset-hash-mismatch");
    }

    [Fact]
    public void R3_MissingAssetBytes_Refuses()
    {
        var store = ResolutionWitness.CreateStore(withAsset: false, withCert: true);
        var cert = ResolutionWitness.CreateSignedCert(WitnessAtomId, WitnessAssetHash);

        var result = PhysicalAtomResolutionVerifier.Verify(
            cert,
            store,
            WitnessIssuerPublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("asset-unresolved");
    }

    [Fact]
    public void R4_TamperedBundleManifestAssetHash_Refuses()
    {
        var bundle = ResolutionWitness.CreateValidBundle();
        var tampered = bundle with
        {
            Certificate = bundle.Certificate with { AssetHash = "0000000000000000000000000000000000000000000000000000000000000000" }
        };

        var result = PhysicalAtomCertBundleVerifier.Verify(tampered, WitnessIssuerPublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("bundle-asset-hash-mismatch");
    }

    [Fact]
    public void A1_ResolvedAssetAndCert_Accepts()
    {
        var store = ResolutionWitness.CreateStore(withAsset: true, withCert: true);
        var cert = ResolutionWitness.CreateSignedCert(WitnessAtomId, WitnessAssetHash);

        var result = PhysicalAtomResolutionVerifier.Verify(
            cert,
            store,
            WitnessIssuerPublicKey);

        result.Trusted.Should().BeTrue();
    }

    [Fact]
    public void A2_ValidBundle_Accepts()
    {
        var bundle = ResolutionWitness.CreateValidBundle();

        var result = PhysicalAtomCertBundleVerifier.Verify(bundle, WitnessIssuerPublicKey);

        result.Trusted.Should().BeTrue();
    }

    private static class ResolutionWitness
    {
        private static readonly byte[] PrivateKey =
            Convert.FromHexString("91A5020FE6CFB499DED63C0EC5B61C37B9353F2005CD1676831D53C6225B7992");

        public static InMemoryAssetResolutionStore CreateStore(bool withAsset, bool withCert)
        {
            var store = new InMemoryAssetResolutionStore();
            if (withAsset)
            {
                store.RegisterAsset(new DigitalTwinAssetRecord(
                    WitnessAssetHash,
                    "1.0.0",
                    "application/octet-stream",
                    WitnessAssetBytes));
            }

            if (withCert)
            {
                store.RegisterCert(CreateSignedCert(WitnessAtomId, WitnessAssetHash));
            }

            return store;
        }

        public static PhysicalAtomCertificate CreateSignedCert(Guid atomId, string assetHash)
        {
            var unsigned = new PhysicalAtomCertificate
            {
                AtomId = atomId,
                BindingScope = BindingScope.Design,
                AssetHash = assetHash,
                AssetVersion = "1.0.0"
            };

            return unsigned with
            {
                IssuerSignature = PhysicalAtomCertificateSigning.Sign(unsigned, PrivateKey)
            };
        }

        public static PhysicalAtomCertBundle CreateValidBundle() =>
            new(
                CreateSignedCert(WitnessAtomId, WitnessAssetHash),
                WitnessAssetBytes,
                "application/octet-stream",
                WitnessIssuerPublicKey);
    }
}
