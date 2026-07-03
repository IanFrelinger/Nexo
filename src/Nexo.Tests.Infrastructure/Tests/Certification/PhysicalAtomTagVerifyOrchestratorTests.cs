using FluentAssertions;
using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;
using Nexo.Certification.Physical.Tagging;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Phase 3 tag-to-verify orchestration tests. Witness fixtures are independent of pipeline tests.
/// </summary>
[Trait("Category", "Certification")]
public sealed class PhysicalAtomTagVerifyOrchestratorTests
{
    private static readonly byte[] WitnessIssuerPublicKey =
        Convert.FromBase64String("SOAn5IcnCADrZ0YtsWCmD6cZacp/xtKi8/iNwPDvjU0=");

    private static readonly byte[] WitnessPrivateKey =
        Convert.FromHexString("91A5020FE6CFB499DED63C0EC5B61C37B9353F2005CD1676831D53C6225B7992");

    private static readonly Guid WitnessAtomId =
        Guid.Parse("6ba7b813-9dad-11d1-80b4-00c04fd430c8");

    private static readonly byte[] WitnessAssetBytes =
        "orchestrator-witness-asset"u8.ToArray();

    private static readonly string WitnessAssetHash =
        AssetContentHasher.ComputeSha256Hex(WitnessAssetBytes);

    [Fact]
    public void R1_MalformedQrPayload_Refuses()
    {
        var store = TagVerifyWitness.CreatePopulatedStore();

        var result = PhysicalAtomTagVerifyOrchestrator.VerifyQr(
            "not-a-valid-tag",
            store,
            WitnessIssuerPublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("tag-prefix-invalid");
    }

    [Fact]
    public void R2_UnregisteredAtom_Refuses()
    {
        var store = new InMemoryAssetResolutionStore();
        var qr = TagVerifyWitness.CreateQr(WitnessAtomId, WitnessAssetHash, "1.0.0");

        var result = PhysicalAtomTagVerifyOrchestrator.VerifyQr(
            qr,
            store,
            WitnessIssuerPublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("atom-unresolved");
    }

    [Fact]
    public void R3_TagReferenceMismatch_Refuses()
    {
        var store = TagVerifyWitness.CreatePopulatedStore();
        var wrongHash = AssetContentHasher.ComputeSha256Hex("other-asset"u8.ToArray());
        var qr = TagVerifyWitness.CreateQr(WitnessAtomId, wrongHash, "1.0.0");

        var result = PhysicalAtomTagVerifyOrchestrator.VerifyQr(
            qr,
            store,
            WitnessIssuerPublicKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("tag-reference-mismatch");
    }

    [Fact]
    public void R4_IssuerFingerprintMismatch_Refuses()
    {
        var store = TagVerifyWitness.CreatePopulatedStore();
        var qr = TagVerifyWitness.CreateQr(WitnessAtomId, WitnessAssetHash, "1.0.0");
        var wrongKey = Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");

        var result = PhysicalAtomTagVerifyOrchestrator.VerifyQr(
            qr,
            store,
            wrongKey);

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("tag-issuer-fingerprint-mismatch");
    }

    [Fact]
    public void A1_ValidQrAgainstStore_Accepts()
    {
        var store = TagVerifyWitness.CreatePopulatedStore();
        var qr = TagVerifyWitness.CreateQr(WitnessAtomId, WitnessAssetHash, "1.0.0");

        var result = PhysicalAtomTagVerifyOrchestrator.VerifyQr(
            qr,
            store,
            WitnessIssuerPublicKey);

        result.Trusted.Should().BeTrue();
    }

    private static class TagVerifyWitness
    {
        public static InMemoryAssetResolutionStore CreatePopulatedStore()
        {
            var store = new InMemoryAssetResolutionStore();
            var cert = CreateSignedCert();
            store.RegisterCert(cert);
            store.RegisterAsset(new DigitalTwinAssetRecord(
                WitnessAssetHash,
                "1.0.0",
                "application/octet-stream",
                WitnessAssetBytes));
            return store;
        }

        public static string CreateQr(Guid atomId, string assetHashHex, string assetVersion)
        {
            var reference = new PhysicalAtomTagReference(
                TagReferenceKind.CertRef,
                atomId,
                Convert.FromHexString(assetHashHex),
                assetVersion,
                IssuerFingerprint.Compute(WitnessIssuerPublicKey));

            return PhysicalAtomQrTagCodec.Encode(reference);
        }

        private static PhysicalAtomCertificate CreateSignedCert()
        {
            var unsigned = new PhysicalAtomCertificate
            {
                AtomId = WitnessAtomId,
                BindingScope = BindingScope.Design,
                AssetHash = WitnessAssetHash,
                AssetVersion = "1.0.0"
            };

            return unsigned with
            {
                IssuerSignature = PhysicalAtomCertificateSigning.Sign(unsigned, WitnessPrivateKey)
            };
        }
    }
}
