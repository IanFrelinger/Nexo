using FluentAssertions;
using Ashlar.Certification.Physical;
using Ashlar.Certification.Physical.Resolution;
using Ashlar.Certification.Physical.Tagging;
using Ashlar.Spatial.Contracts;
using Ashlar.Spatial.Runtime;
using Ashlar.Spatial.Runtime.Ports;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Spatial;

/// <summary>Tests for spatial binding service rejection.</summary>
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

    /// <summary>Creates provider.</summary>
    /// <param name="atomId">Atom id.</param>
    /// <param name="TrackingState.Tracking">Tracking state.tracking.</param>
    private static FakeSpatialAnchorProvider CreateProvider(string atomId, TrackingState state = TrackingState.Tracking) =>
        new(new[]
        {
            new ScriptedAtomSequence
            {
                AtomId = atomId,
                Samples = new[] { CreateSample(T0, state) }
            }
        });

    /// <summary>Creates sample.</summary>
    /// <param name="timestamp">Timestamp.</param>
    /// <param name="state">State.</param>
    private static PoseSample CreateSample(DateTimeOffset timestamp, TrackingState state) =>
        new(new SpatialVector3(0, 0, 0), new SpatialQuaternion(0, 0, 0, 1), 0.95, timestamp, state);
}
