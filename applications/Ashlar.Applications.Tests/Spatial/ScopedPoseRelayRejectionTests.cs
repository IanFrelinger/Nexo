using FluentAssertions;
using Ashlar.Spatial.Contracts;
using Ashlar.Spatial.Multiplayer;
using Ashlar.Spatial.Runtime;
using Ashlar.Spatial.Runtime.Ports;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Spatial;

/// <summary>Tests for scoped pose relay rejection.</summary>
[Trait("Category", "Spatial")]
public sealed class ScopedPoseRelayRejectionTests
{
    private const string ScopeId = "scope-lan-1";
    private static readonly DateTimeOffset T0 = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void R1_NonHostPublish_ExplicitRejection()
    {
        using var transport = new InMemoryScopedPoseTransport();
        var store = new InMemoryMatchScopeStore();
        store.TryCreate(ScopeId, "host-a", new[] { "atom-a" });
        store.TryJoin(ScopeId, "guest-b");
        var relay = new ScopedPoseRelay(store, transport);

        var result = relay.Host.TryPublishPose(
            ScopeId,
            "guest-b",
            "atom-a",
            CreatePose(T0));

        result.Accepted.Should().BeFalse();
        result.RejectionCode.Should().Be("not-scope-host");
    }

    [Fact]
    public void R2_HostDisconnect_ParticipantsReceiveExplicitLostSignal()
    {
        using var transport = new InMemoryScopedPoseTransport();
        var store = new InMemoryMatchScopeStore();
        store.TryCreate(ScopeId, "host-a", new[] { "atom-a" });
        store.TryJoin(ScopeId, "guest-b");
        var relay = new ScopedPoseRelay(store, transport);

        var subscribe = relay.Participants.TrySubscribe(ScopeId, "guest-b");
        subscribe.Succeeded.Should().BeTrue();

        var messages = new List<ScopedPoseMessage>();
        using var sub = subscribe.Stream!.Subscribe(messages.Add);

        relay.Host.PublishHostLost(ScopeId, "atom-a", "host-disconnected");

        messages.Should().ContainSingle(m =>
            m.Lost
            && m.AtomId == "atom-a"
            && m.SignalReason == "host-disconnected"
            && m.Pose == null);
    }

    [Fact]
    public void R3_LateJoiner_ReceivesMembershipOnlyNoHistoricalPoses()
    {
        using var transport = new InMemoryScopedPoseTransport();
        var store = new InMemoryMatchScopeStore();
        store.TryCreate(ScopeId, "host-a", new[] { "atom-a", "atom-b" });
        store.TryJoin(ScopeId, "guest-b");

        var relay = new ScopedPoseRelay(store, transport);
        relay.Host.TryPublishPose(ScopeId, "host-a", "atom-a", CreatePose(T0));

        var snapshot = relay.Participants.TryGetScopeSnapshot(ScopeId, "guest-b");

        snapshot.Succeeded.Should().BeTrue();
        snapshot.Snapshot!.ScopedAtomIds.Should().BeEquivalentTo(new[] { "atom-a", "atom-b" });
        snapshot.Snapshot.MemberParticipantIds.Should().Contain(new[] { "host-a", "guest-b" });

        var lateSubscribe = relay.Participants.TrySubscribe(ScopeId, "guest-b");
        var received = new List<ScopedPoseMessage>();
        using var sub = lateSubscribe.Stream!.Subscribe(received.Add);
        received.Should().BeEmpty("late joiners must not receive historical pose replay");
    }

    [Fact]
    public void R4_DuplicateHostClaim_FirstWriterWinsDeterministically()
    {
        var store = new InMemoryMatchScopeStore();

        var first = store.TryCreate(ScopeId, "host-a", new[] { "atom-a" });
        var second = store.TryCreate(ScopeId, "host-b", new[] { "atom-a" });

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeFalse();
        second.RejectionCode.Should().Be("host-already-claimed");
        store.GetScope(ScopeId)!.HostParticipantId.Should().Be("host-a");
    }

    [Fact]
    public void R5_NonMemberSubscribe_RejectedNoPoseLeak()
    {
        using var transport = new InMemoryScopedPoseTransport();
        var store = new InMemoryMatchScopeStore();
        store.TryCreate(ScopeId, "host-a", new[] { "atom-a" });
        var relay = new ScopedPoseRelay(store, transport);

        relay.Host.TryPublishPose(ScopeId, "host-a", "atom-a", CreatePose(T0));

        var subscribe = relay.Participants.TrySubscribe(ScopeId, "intruder-c");

        subscribe.Succeeded.Should().BeFalse();
        subscribe.RejectionCode.Should().Be("not-scope-member");
        subscribe.Stream.Should().BeNull();
    }

    [Fact]
    public void A1_HostBindingBridge_RelaysCertifiedPoseToMember()
    {
        var identity = SpatialIdentityFixtures.Create("atom-a");
        var resolver = new FakePhysicalAtomResolver(new[] { ("marker-a", identity) });
        using var provider = new FakeSpatialAnchorProvider(new[]
        {
            new ScriptedAtomSequence
            {
                AtomId = identity.AtomId,
                Samples = new[] { CreatePose(T0) }
            }
        });
        using var binding = new SpatialBindingService(resolver, provider);

        using var transport = new InMemoryScopedPoseTransport();
        var store = new InMemoryMatchScopeStore();
        store.TryCreate(ScopeId, "host-a", new[] { identity.AtomId });
        store.TryJoin(ScopeId, "guest-b");

        var relay = new ScopedPoseRelay(store, transport);
        using var bridge = new SpatialHostRelayBridge(relay);

        bridge.AttachHostBinding(ScopeId, "host-a", identity.AtomId, "marker-a", binding)
            .Succeeded.Should().BeTrue();

        var subscribe = relay.Participants.TrySubscribe(ScopeId, "guest-b");
        var messages = new List<ScopedPoseMessage>();
        using var sub = subscribe.Stream!.Subscribe(messages.Add);

        provider.PublishNext(identity.AtomId);

        messages.Should().ContainSingle(m => !m.Lost && m.Pose != null);
    }

    [Fact]
    public void R6_BridgeRejectsNonHostAttach()
    {
        var identity = SpatialIdentityFixtures.Create("atom-a");
        var resolver = new FakePhysicalAtomResolver(new[] { ("marker-a", identity) });
        using var binding = new SpatialBindingService(resolver, new FakeSpatialAnchorProvider(Array.Empty<ScriptedAtomSequence>()));
        using var transport = new InMemoryScopedPoseTransport();
        var store = new InMemoryMatchScopeStore();
        store.TryCreate(ScopeId, "host-a", new[] { identity.AtomId });
        store.TryJoin(ScopeId, "guest-b");
        var relay = new ScopedPoseRelay(store, transport);
        using var bridge = new SpatialHostRelayBridge(relay);

        var result = bridge.AttachHostBinding(ScopeId, "guest-b", identity.AtomId, "marker-a", binding);

        result.Succeeded.Should().BeFalse();
        result.RejectionCode.Should().Be("not-scope-host");
    }

    [Fact]
    public void R7_BridgeRejectsAtomNotInScope()
    {
        var identity = SpatialIdentityFixtures.Create("atom-a");
        var resolver = new FakePhysicalAtomResolver(new[] { ("marker-a", identity) });
        using var binding = new SpatialBindingService(resolver, new FakeSpatialAnchorProvider(Array.Empty<ScriptedAtomSequence>()));
        using var transport = new InMemoryScopedPoseTransport();
        var store = new InMemoryMatchScopeStore();
        store.TryCreate(ScopeId, "host-a", new[] { "other-atom" });
        var relay = new ScopedPoseRelay(store, transport);
        using var bridge = new SpatialHostRelayBridge(relay);

        var result = bridge.AttachHostBinding(ScopeId, "host-a", identity.AtomId, "marker-a", binding);

        result.Succeeded.Should().BeFalse();
        result.RejectionCode.Should().Be("atom-not-in-scope");
    }

    [Fact]
    public void R8_BridgeRejectsMissingScope()
    {
        var identity = SpatialIdentityFixtures.Create("atom-a");
        var resolver = new FakePhysicalAtomResolver(new[] { ("marker-a", identity) });
        using var binding = new SpatialBindingService(resolver, new FakeSpatialAnchorProvider(Array.Empty<ScriptedAtomSequence>()));
        using var transport = new InMemoryScopedPoseTransport();
        var store = new InMemoryMatchScopeStore();
        var relay = new ScopedPoseRelay(store, transport);
        using var bridge = new SpatialHostRelayBridge(relay);

        var result = bridge.AttachHostBinding("missing-scope", "host-a", identity.AtomId, "marker-a", binding);

        result.Succeeded.Should().BeFalse();
        result.RejectionCode.Should().Be("scope-not-found");
    }

    [Fact]
    public void R9_BridgeRejectsDuplicateAttach()
    {
        var identity = SpatialIdentityFixtures.Create("atom-a");
        var resolver = new FakePhysicalAtomResolver(new[] { ("marker-a", identity) });
        using var provider = new FakeSpatialAnchorProvider(new[]
        {
            new ScriptedAtomSequence
            {
                AtomId = identity.AtomId,
                Samples = new[] { CreatePose(T0) }
            }
        });
        using var binding = new SpatialBindingService(resolver, provider);
        using var transport = new InMemoryScopedPoseTransport();
        var store = new InMemoryMatchScopeStore();
        store.TryCreate(ScopeId, "host-a", new[] { identity.AtomId });
        var relay = new ScopedPoseRelay(store, transport);
        using var bridge = new SpatialHostRelayBridge(relay);

        bridge.AttachHostBinding(ScopeId, "host-a", identity.AtomId, "marker-a", binding).Succeeded.Should().BeTrue();
        bridge.AttachHostBinding(ScopeId, "host-a", identity.AtomId, "marker-a", binding).RejectionCode
            .Should().Be("binding-already-attached");
    }

    /// <summary>Creates pose.</summary>
    /// <param name="timestamp">Timestamp.</param>
    private static PoseSample CreatePose(DateTimeOffset timestamp) =>
        new(new SpatialVector3(1, 2, 3), new SpatialQuaternion(0, 0, 0, 1), 0.95, timestamp, TrackingState.Tracking);
}
