using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Fleet.Models;
using Nexo.Core.Application.Fleet.Ports;
using Nexo.Infrastructure.Fleet;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Fleet;

public sealed class MeshTaskPlacementServiceTests
{
    [Fact]
    public async Task Schedule_assigns_node_with_required_brick_and_respects_drain()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = new MeshTaskPlacementService(nodes, tasks, NullLogger<MeshTaskPlacementService>.Instance);

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
        var placement = new MeshTaskPlacementService(nodes, tasks, NullLogger<MeshTaskPlacementService>.Instance);

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
        var placement = new MeshTaskPlacementService(nodes, tasks, NullLogger<MeshTaskPlacementService>.Instance);
        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "p", "https://p/", new Dictionary<string, string>(), new[] { "b" },
            false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var task = await tasks.CreateAsync(new MeshTaskCreateSpec(null, 1, new[] { "b" }, null, 0, null));
        var (ok1, t1, _) = await placement.TryScheduleAsync(task.TaskId, "sched-key", null);
        ok1.Should().BeTrue();
        var (ok2, t2, _) = await placement.TryScheduleAsync(task.TaskId, "sched-key", null);
        ok2.Should().BeTrue();
        t2!.AssignedPeerId.Should().Be(t1!.AssignedPeerId);
        t2.AttemptCount.Should().Be(t1.AttemptCount);
    }

    [Fact]
    public async Task Schedule_with_different_idempotency_key_while_assigned_returns_conflict()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = new MeshTaskPlacementService(nodes, tasks, NullLogger<MeshTaskPlacementService>.Instance);
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
}

