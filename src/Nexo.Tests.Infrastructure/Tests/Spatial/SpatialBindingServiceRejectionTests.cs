using FluentAssertions;
using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;
using Nexo.Certification.Physical.Tagging;
using Nexo.Spatial.Contracts;
using Nexo.Spatial.Runtime;
using Nexo.Spatial.Runtime.Ports;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Spatial;

[Trait("Category", "Spatial")]
public sealed class SpatialBindingServiceRejectionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void R1_UncertifiedAtom_ExplicitRejectionNotPendingNull()
    {
        var identity = SpatialIdentityFixtures.Create("atom-a");
        var resolver = new FakePhysicalAtomResolver(Array.Empty<(string, ResolvedAtomIdentity)>());
        using var provider = CreateProvider(identity.AtomId);
        using var service = new SpatialBindingService(resolver, provider);

        var result = service.TryBind("marker-a");

        result.Active.Should().BeFalse();
        result.RejectionCode.Should().Be("atom-not-certified");
        result.ProcessedPose.Should().BeNull();
    }

    [Fact]
    public void R2_ProviderLost_SurfacesLostWithoutFabricatedPose()
    {
        var identity = SpatialIdentityFixtures.Create("atom-a");
        var resolver = new FakePhysicalAtomResolver(new[] { ("marker-a", identity) });
        using var provider = CreateProvider(identity.AtomId, TrackingState.Lost);
        using var service = new SpatialBindingService(resolver, provider);

        var result = service.TryBind("marker-a");

        result.Active.Should().BeFalse();
        result.RejectionCode.Should().Be("provider-lost");
    }

    [Fact]
    public void R3_AssetHashChangesMidStream_ReFailsClosed()
    {
        var identity = SpatialIdentityFixtures.Create("atom-a");
        var resolver = new FakePhysicalAtomResolver(new[] { ("marker-a", identity) });
        using var provider = new FakeSpatialAnchorProvider(new[]
        {
            new ScriptedAtomSequence
            {
                AtomId = identity.AtomId,
                Samples = new[] { CreateSample(T0, TrackingState.Tracking) }
            }
        });
        using var service = new SpatialBindingService(resolver, provider);

        var updates = new List<SpatialBindingUpdate>();
        using var sub = service.ObserveBoundPose(identity.AtomId, "marker-a").Subscribe(updates.Add);

        provider.PublishNext(identity.AtomId);
        updates.Should().ContainSingle(u => u.Active);

        resolver.ReplaceIdentity("marker-a", identity with { AssetHash = new string('b', 64) });
        provider.Publish(identity.AtomId, CreateSample(T0.AddSeconds(1), TrackingState.Tracking));

        updates.Should().Contain(u => u.RejectionCode == "asset-hash-changed");
    }

    private static FakeSpatialAnchorProvider CreateProvider(string atomId, TrackingState state = TrackingState.Tracking) =>
        new(new[]
        {
            new ScriptedAtomSequence
            {
                AtomId = atomId,
                Samples = new[] { CreateSample(T0, state) }
            }
        });

    private static PoseSample CreateSample(DateTimeOffset timestamp, TrackingState state) =>
        new(new SpatialVector3(0, 0, 0), new SpatialQuaternion(0, 0, 0, 1), 0.95, timestamp, state);
}

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

    private sealed class ThrowingAssetResolutionStore : IAssetResolutionStore
    {
        public bool TryResolveCert(Guid atomId, out PhysicalAtomCertificate? certificate)
        {
            throw new InvalidDataException("Simulated codec/store failure.");
        }

        public bool TryResolveAsset(string assetHash, string assetVersion, out DigitalTwinAssetRecord? asset)
        {
            throw new InvalidDataException("Simulated codec/store failure.");
        }
    }
}
