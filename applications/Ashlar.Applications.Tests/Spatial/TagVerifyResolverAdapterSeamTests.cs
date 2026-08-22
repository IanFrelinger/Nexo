using FluentAssertions;
using Ashlar.Certification.Physical;
using Ashlar.Certification.Physical.Resolution;
using Ashlar.Certification.Physical.Tagging;
using Ashlar.Spatial.Contracts;
using Ashlar.Spatial.Runtime;
using Ashlar.Spatial.Runtime.Ports;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Spatial;

/// <summary>Tests for tag verify resolver adapter seam.</summary>
[Trait("Category", "Spatial")]
public sealed class TagVerifyResolverAdapterSeamTests
{
    private static readonly byte[] WitnessIssuerPublicKey =
        Convert.FromBase64String("SOAn5IcnCADrZ0YtsWCmD6cZacp/xtKi8/iNwPDvjU0=");

    private static readonly byte[] WitnessPrivateKey =
        Convert.FromHexString("91A5020FE6CFB499DED63C0EC5B61C37B9353F2005CD1676831D53C6225B7992");

    private static readonly Guid WitnessAtomId =
        Guid.Parse("6ba7b813-9dad-11d1-80b4-00c04fd430c8");

    private static readonly byte[] WitnessAssetBytes =
        "spatial-seam-witness-asset"u8.ToArray();

    private static readonly string WitnessAssetHash =
        AssetContentHasher.ComputeSha256Hex(WitnessAssetBytes);

    [Fact]
    public void R1_IssuerFingerprintMismatch_SurfacesAtomNotCertifiedThroughBinding()
    {
        var store = CreatePopulatedStore();
        var qr = CreateQr(WitnessAtomId, WitnessAssetHash, "1.0.0");
        var wrongKey = Convert.FromBase64String("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
        var resolver = new TagVerifyResolverAdapter(store, wrongKey);
        var atomId = WitnessAtomId.ToString("D");
        using var provider = CreateProvider(atomId);
        using var service = new SpatialBindingService(resolver, provider);

        var result = service.TryBind(qr);

        result.Active.Should().BeFalse();
        result.RejectionCode.Should().Be("atom-not-certified");
    }

    [Fact]
    public void R2_OrchestratorThrows_ReturnsNotFoundWithoutPoseLayerException()
    {
        var store = new ThrowingAssetResolutionStore();
        var qr = CreateQr(WitnessAtomId, WitnessAssetHash, "1.0.0");
        var resolver = new TagVerifyResolverAdapter(store, WitnessIssuerPublicKey);

        PhysicalAtomResolveResult resolveResult = default!;
        Action act = () => resolveResult = resolver.Resolve(qr);

        act.Should().NotThrow();
        resolveResult.Found.Should().BeFalse();

        using var provider = CreateProvider(WitnessAtomId.ToString("D"));
        using var service = new SpatialBindingService(resolver, provider);
        service.TryBind(qr).RejectionCode.Should().Be("atom-not-certified");
    }

    /// <summary>Creates provider.</summary>
    /// <param name="atomId">Atom id.</param>
    private static FakeSpatialAnchorProvider CreateProvider(string atomId) =>
        new(new[]
        {
            new ScriptedAtomSequence
            {
                AtomId = atomId,
                Samples = new[]
                {
                    new PoseSample(
                        new SpatialVector3(0, 0, 0),
                        new SpatialQuaternion(0, 0, 0, 1),
                        0.95,
                        DateTimeOffset.UtcNow,
                        TrackingState.Tracking)
                }
            }
        });

    private static InMemoryAssetResolutionStore CreatePopulatedStore()
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

    private static string CreateQr(Guid atomId, string assetHashHex, string assetVersion)
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

    /// <summary>Tests for throwing asset resolution store.</summary>
    private sealed class ThrowingAssetResolutionStore : IAssetResolutionStore
    {
        public bool TryResolveCert(Guid atomId, out PhysicalAtomCertificate? certificate)
        {
            /// <summary>Invalid data exception.</summary>
            /// <param name="failure."">Failure.".</param>
            throw new InvalidDataException("Simulated codec/store failure.");
        }

        public bool TryResolveAsset(string assetHash, string assetVersion, out DigitalTwinAssetRecord? asset)
        {
            /// <summary>Invalid data exception.</summary>
            /// <param name="failure."">Failure.".</param>
            throw new InvalidDataException("Simulated codec/store failure.");
        }
    }
}
