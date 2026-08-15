using FluentAssertions;
using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;
using Nexo.Certification.Physical.Tagging;
using Nexo.Spatial.Contracts;
using Nexo.Spatial.Multiplayer;
using Nexo.Spatial.Runtime;
using Nexo.Spatial.Runtime.Ports;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Spatial;

/// <summary>Tests for spatial runtime coverage.</summary>
[Trait("Category", "Spatial")]
public sealed class SpatialRuntimeCoverageTests
{
    private static readonly byte[] WitnessIssuerPublicKey =
        Convert.FromBase64String("SOAn5IcnCADrZ0YtsWCmD6cZacp/xtKi8/iNwPDvjU0=");

    private static readonly byte[] WitnessPrivateKey =
        Convert.FromHexString("91A5020FE6CFB499DED63C0EC5B61C37B9353F2005CD1676831D53C6225B7992");

    private static readonly Guid WitnessAtomId =
        Guid.Parse("6ba7b813-9dad-11d1-80b4-00c04fd430c8");

    [Fact]
    public void A1_TagVerifyAdapter_ResolvesTrustedQrToIdentity()
    {
        var store = CreatePopulatedStore();
        var qr = CreateQr(WitnessAtomId, AssetContentHasher.ComputeSha256Hex("spatial-coverage-asset"u8.ToArray()), "1.0.0");
        var adapter = new TagVerifyResolverAdapter(store, WitnessIssuerPublicKey);

        var result = adapter.Resolve(qr);

        result.Found.Should().BeTrue();
        result.Record!.AtomId.Should().Be(WitnessAtomId.ToString("D"));
        result.Record.AssetVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void R1_ObserveBoundPose_AtomIdMismatch_RejectsUpdate()
    {
        var identity = SpatialIdentityFixtures.Create("atom-a");
        var resolver = new FakePhysicalAtomResolver(new[] { ("marker-a", identity) });
        using var provider = new FakeSpatialAnchorProvider(Array.Empty<ScriptedAtomSequence>());
        using var service = new SpatialBindingService(resolver, provider);

        var updates = new List<SpatialBindingUpdate>();
        using var sub = service.ObserveBoundPose("other-atom", "marker-a").Subscribe(updates.Add);

        updates.Should().ContainSingle(u => u.RejectionCode == "atom-id-mismatch");
    }

    [Fact]
    public void R2_BridgeForwardsBindingLostSignalToParticipants()
    {
        var identity = SpatialIdentityFixtures.Create("atom-a");
        var resolver = new FakePhysicalAtomResolver(new[] { ("marker-a", identity) });
        using var provider = new FakeSpatialAnchorProvider(new[]
        {
            new ScriptedAtomSequence
            {
                AtomId = identity.AtomId,
                Samples = new[]
                {
                    new PoseSample(
                        new SpatialVector3(0, 0, 0),
                        new SpatialQuaternion(0, 0, 0, 1),
                        0.95,
                        DateTimeOffset.UtcNow,
                        TrackingState.Lost)
                }
            }
        });
        using var binding = new SpatialBindingService(resolver, provider);
        using var transport = new InMemoryScopedPoseTransport();
        var store = new InMemoryMatchScopeStore();
        store.TryCreate("scope-1", "host-a", new[] { identity.AtomId });
        store.TryJoin("scope-1", "guest-b");
        var relay = new ScopedPoseRelay(store, transport);
        using var bridge = new SpatialHostRelayBridge(relay);

        bridge.AttachHostBinding("scope-1", "host-a", identity.AtomId, "marker-a", binding).Succeeded.Should().BeTrue();

        var messages = new List<ScopedPoseMessage>();
        using var sub = relay.Participants.TrySubscribe("scope-1", "guest-b").Stream!.Subscribe(messages.Add);
        provider.PublishNext(identity.AtomId);

        messages.Should().Contain(m => m.Lost && m.SignalReason == "provider-lost");
    }

    [Fact]
    public async Task A2_FakeProvider_FromJsonScript_ReplaysPose()
    {
        const string json = """
            [
              {
                "atomId": "atom-json",
                "samples": [
                  {
                    "position": { "x": 1, "y": 2, "z": 3 },
                    "rotation": { "x": 0, "y": 0, "z": 0, "w": 1 },
                    "confidence": 0.9,
                    "timestamp": "2026-06-01T12:00:00Z",
                    "trackingState": 0
                  }
                ]
              }
            ]
            """;

        using var provider = FakeSpatialAnchorProvider.FromJson(json);
        var pose = await provider.GetCurrentPose("atom-json");

        pose.Should().NotBeNull();
        pose!.Position.X.Should().Be(1);
    }

    private static InMemoryAssetResolutionStore CreatePopulatedStore()
    {
        var assetBytes = "spatial-coverage-asset"u8.ToArray();
        var assetHash = AssetContentHasher.ComputeSha256Hex(assetBytes);
        var store = new InMemoryAssetResolutionStore();
        var unsigned = new PhysicalAtomCertificate
        {
            AtomId = WitnessAtomId,
            BindingScope = BindingScope.Design,
            AssetHash = assetHash,
            AssetVersion = "1.0.0"
        };
        store.RegisterCert(unsigned with
        {
            IssuerSignature = PhysicalAtomCertificateSigning.Sign(unsigned, WitnessPrivateKey)
        });
        store.RegisterAsset(new DigitalTwinAssetRecord(assetHash, "1.0.0", "application/octet-stream", assetBytes));
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
}
