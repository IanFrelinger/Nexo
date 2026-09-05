using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Contracts.Ports;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for mesh task placement service.</summary>
public sealed class MeshTaskPlacementServiceTests
{
    private static MeshTaskPlacementService CreatePlacement(
        InMemoryFleetNodeRegistry nodes,
        InMemoryMeshTaskRegistry tasks,
        string peerTrustPolicy = "any") =>
        new(
            nodes,
            tasks,
            Options.Create(new MeshCheckpointOptions()),
            Options.Create(new MeshPlacementTrustOptions { PeerTrustPolicy = peerTrustPolicy }),
            NullLogger<MeshTaskPlacementService>.Instance);

    [Fact]
    public async Task Schedule_assigns_node_with_required_brick_and_respects_drain()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "peer-a",
            "https://a.example/",
            new Dictionary<string, string>(),
            new[] { "brick-x" },
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow));

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "peer-b",
            "https://b.example/",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow));

        var task = await tasks.CreateAsync(new MeshTaskCreateSpec(
            Name: "t1",
            Steps: 1,
            RequiredBrickIds: new[] { "brick-x" },
            Affinity: null,
            Priority: 0,
            DeadlineUtc: null));

        var (ok, placed, err) = await placement.TryScheduleAsync(task.TaskId);
        ok.Should().BeTrue(err);
        placed!.AssignedPeerId.Should().Be("peer-a");
        placed.AssignedApiBaseUrl.Should().Be("https://a.example/");

        await nodes.SetDrainedAsync("peer-a", true);
        var task2 = await tasks.CreateAsync(new MeshTaskCreateSpec("t2", 1, new[] { "brick-x" }, null, 0, null));
        var (ok2, placed2, _) = await placement.TryScheduleAsync(task2.TaskId);
        ok2.Should().BeFalse("no eligible nodes when only brick holder is drained");
        placed2!.Status.Should().Be(MeshTaskStatus.Pending);
    }

    [Fact]
    public async Task Retry_skips_previously_assigned_peer_when_alternative_exists()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "peer-1", "https://1.example/", new Dictionary<string, string>(), new[] { "b" },
            false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "peer-2", "https://2.example/", new Dictionary<string, string>(), new[] { "b" },
            false, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow));

        var task = await tasks.CreateAsync(new MeshTaskCreateSpec(null, 1, new[] { "b" }, null, 0, null));
        var (ok1, t1, _) = await placement.TryScheduleAsync(task.TaskId);
        ok1.Should().BeTrue();
        t1!.AssignedPeerId.Should().Be("peer-1", "same queue depth; newer heartbeat then stable peer id tie-break");

        var (ok2, t2, _) = await placement.TryRetryAsync(task.TaskId);
        ok2.Should().BeTrue();
        t2!.AssignedPeerId.Should().Be("peer-2");
    }

    [Fact]
    public async Task Schedule_with_same_idempotency_key_is_idempotent()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);
        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "p", "https://p/", new Dictionary<string, string>(), new[] { "b" },
            false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var task = await tasks.CreateAsync(new MeshTaskCreateSpec(null, 1, new[] { "b" }, null, 0, null));
        var (ok1, t1, _) = await placement.TryScheduleAsync(task.TaskId, "sched-key", null);
        ok1.Should().BeTrue();
        var (ok2, t2, _) = await placement.TryScheduleAsync(task.TaskId, "sched-key", null);
        ok2.Should().BeTrue();
        t2!.AssignedPeerId.Should().Be(t1!.AssignedPeerId);
        Assert.Equal(t1.AttemptCount, t2.AttemptCount);
    }

    [Fact]
    public async Task Schedule_with_different_idempotency_key_while_assigned_returns_conflict()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);
        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "p", "https://p/", new Dictionary<string, string>(), new[] { "b" },
            false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var task = await tasks.CreateAsync(new MeshTaskCreateSpec(null, 1, new[] { "b" }, null, 0, null));
        var (ok1, _, _) = await placement.TryScheduleAsync(task.TaskId, "key-a", null);
        ok1.Should().BeTrue();
        var (ok2, _, err) = await placement.TryScheduleAsync(task.TaskId, "key-b", null);
        ok2.Should().BeFalse();
        err.Should().Be("schedule.idempotency_conflict");
    }

    [Fact]
    public async Task Schedule_while_running_with_different_idempotency_key_returns_conflict()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);
        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "p", "https://p/", new Dictionary<string, string>(), new[] { "b" },
            false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var task = await tasks.CreateAsync(new MeshTaskCreateSpec(null, 1, new[] { "b" }, null, 0, null));
        var (ok1, placed, _) = await placement.TryScheduleAsync(task.TaskId, "key-a", null);
        ok1.Should().BeTrue();
        var running = placed! with { Status = MeshTaskStatus.Running };
        await tasks.UpdateAsync(running);

        var (ok2, _, err) = await placement.TryScheduleAsync(task.TaskId, "key-b", null);
        ok2.Should().BeFalse();
        err.Should().Be("schedule.idempotency_conflict");
    }

    [Fact]
    public async Task Schedule_reclaims_expired_lease_on_running_task()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);
        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "p", "https://p/", new Dictionary<string, string>(), new[] { "b" },
            false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var task = await tasks.CreateAsync(new MeshTaskCreateSpec(null, 1, new[] { "b" }, null, 0, null));
        var (ok1, placed, _) = await placement.TryScheduleAsync(task.TaskId, null, null, leaseSecondsOverride: 60);
        ok1.Should().BeTrue();
        var stale = placed! with
        {
            Status = MeshTaskStatus.Running,
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(-10)
        };
        await tasks.UpdateAsync(stale);

        var (ok2, next, _) = await placement.TryScheduleAsync(task.TaskId, "new-key", null);
        ok2.Should().BeTrue();
        next!.Status.Should().Be(MeshTaskStatus.Assigned);
        next.LastScheduleIdempotencyKey.Should().Be("new-key");
    }

    [Fact]
    public async Task Schedule_trusted_only_policy_skips_untrusted_peers()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks, peerTrustPolicy: "trusted-only");

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "untrusted-peer",
            "https://u.example/",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            0,
            MeshFleetTrustTier.Untrusted));
        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "trusted-peer",
            "https://t.example/",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            0,
            MeshFleetTrustTier.Trusted));

        var task = await tasks.CreateAsync(new MeshTaskCreateSpec("trust-test", 1, Array.Empty<string>(), null, 0, null));
        var (ok, placed, _) = await placement.TryScheduleAsync(task.TaskId);
        ok.Should().BeTrue();
        placed!.AssignedPeerId.Should().Be("trusted-peer");

        await nodes.RemoveAsync("trusted-peer");
        var task2 = await tasks.CreateAsync(new MeshTaskCreateSpec("trust-blocked", 1, Array.Empty<string>(), null, 0, null));
        var (ok2, pending, err) = await placement.TryScheduleAsync(task2.TaskId);
        ok2.Should().BeFalse();
        err.Should().Be("placement.trust_policy_blocked");
        pending!.PlacementReason.Should().Be("placement.trust_policy_blocked");
    }

    [Fact]
    public async Task Schedule_revoked_peer_not_placed()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "revoked-peer",
            "https://r.example/",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            0,
            MeshFleetTrustTier.Trusted,
            Admitted: false));

        var task = await tasks.CreateAsync(new MeshTaskCreateSpec("gov-test", 1, Array.Empty<string>(), null, 0, null));
        var (ok, pending, err) = await placement.TryScheduleAsync(task.TaskId);
        ok.Should().BeFalse();
        err.Should().Be("placement.peer_not_admitted");
        pending!.PlacementReason.Should().Be("placement.peer_not_admitted");
    }

    [Fact]
    public async Task Concurrent_schedule_assigns_one_lease()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "peer-a", "https://a.example/", new Dictionary<string, string>(), Array.Empty<string>(),
            false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "peer-b", "https://b.example/", new Dictionary<string, string>(), Array.Empty<string>(),
            false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var task = await tasks.CreateAsync(new MeshTaskCreateSpec("race", 1, Array.Empty<string>(), null, 0, null));

        var first = placement.TryScheduleAsync(task.TaskId);
        var second = placement.TryScheduleAsync(task.TaskId);
        await Task.WhenAll(first, second);

        first.Result.Ok.Should().BeTrue(first.Result.Error);
        second.Result.Ok.Should().BeTrue(second.Result.Error);
        first.Result.Task!.LeaseToken.Should().Be(second.Result.Task!.LeaseToken);
        first.Result.Task.AssignedPeerId.Should().Be(second.Result.Task.AssignedPeerId);

        var stored = await tasks.GetAsync(task.TaskId);
        stored!.Status.Should().Be(MeshTaskStatus.Assigned);
        stored.LeaseToken.Should().Be(first.Result.Task.LeaseToken);
        stored.AttemptCount.Should().Be(1);
    }
}

