using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Tests for mesh task placement service gap coverage.</summary>
public sealed class MeshTaskPlacementServiceGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var checkpoint = Options.Create(new MeshCheckpointOptions());
        var trust = Options.Create(new MeshPlacementTrustOptions());

        var act = () => new MeshTaskPlacementService(null!, tasks, checkpoint, trust);
        act.Should().Throw<ArgumentNullException>().WithParameterName("nodes");

        act = () => new MeshTaskPlacementService(nodes, null!, checkpoint, trust);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tasks");

        act = () => new MeshTaskPlacementService(nodes, tasks, null!, trust);
        act.Should().Throw<ArgumentNullException>().WithParameterName("checkpointOptions");
    }

    [Fact]
    public async Task TryScheduleAsync_returns_not_found_for_missing_task()
    {
        var placement = CreatePlacement(new InMemoryFleetNodeRegistry(), new InMemoryMeshTaskRegistry());

        var (ok, task, error) = await placement.TryScheduleAsync("missing-task");

        ok.Should().BeFalse();
        task.Should().BeNull();
        error.Should().Be("task.not_found");
    }

    [Fact]
    public async Task TryScheduleAsync_rejects_already_succeeded_task()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);
        /// <summary>Register eligible node.</summary>
        await RegisterEligibleNode(nodes);

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("done", 1, Array.Empty<string>(), null, 0, null));
        await tasks.UpdateAsync(created with { Status = MeshTaskStatus.Succeeded });

        var (ok, task, error) = await placement.TryScheduleAsync(created.TaskId);

        ok.Should().BeFalse();
        error.Should().Be("task.already_succeeded");
        task!.Status.Should().Be(MeshTaskStatus.Succeeded);
    }

    [Fact]
    public async Task TryScheduleAsync_marks_deadline_exceeded_tasks_failed()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);
        /// <summary>Register eligible node.</summary>
        await RegisterEligibleNode(nodes);

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec(
            "late",
            1,
            Array.Empty<string>(),
            null,
            0,
            DeadlineUtc: DateTimeOffset.UtcNow.AddMinutes(-1)));

        var (ok, task, error) = await placement.TryScheduleAsync(created.TaskId);

        ok.Should().BeFalse();
        error.Should().Be("task.deadline_exceeded");
        task!.Status.Should().Be(MeshTaskStatus.Failed);
        task.PlacementReason.Should().Be("deadline_exceeded");
    }

    [Fact]
    public async Task TryScheduleAsync_returns_no_eligible_nodes_when_registry_empty()
    {
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(new InMemoryFleetNodeRegistry(), tasks);
        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("orphan", 1, Array.Empty<string>(), null, 0, null));

        var (ok, pending, error) = await placement.TryScheduleAsync(created.TaskId);

        ok.Should().BeFalse();
        error.Should().Be("placement.no_eligible_nodes");
        pending!.PlacementReason.Should().Be("placement.no_eligible_nodes");
    }

    [Fact]
    public async Task TryScheduleAsync_skips_nodes_with_blank_api_base_url()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "blank-url",
            "   ",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow));

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("needs-node", 1, Array.Empty<string>(), null, 0, null));
        var (ok, pending, error) = await placement.TryScheduleAsync(created.TaskId);

        ok.Should().BeFalse();
        error.Should().Be("placement.no_eligible_nodes");
        pending!.Status.Should().Be(MeshTaskStatus.Pending);
    }

    [Fact]
    public async Task TryScheduleAsync_requires_matching_affinity_labels()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "peer-a",
            "https://a.example/",
            new Dictionary<string, string> { ["zone"] = "west" },
            Array.Empty<string>(),
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow));

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec(
            "affinity",
            1,
            Array.Empty<string>(),
            new Dictionary<string, string> { ["zone"] = "east" },
            0,
            null));

        var (ok, pending, error) = await placement.TryScheduleAsync(created.TaskId);

        ok.Should().BeFalse();
        error.Should().Be("placement.no_eligible_nodes");
        pending!.PlacementReason.Should().Be("placement.no_eligible_nodes");
    }

    [Fact]
    public async Task TryScheduleAsync_accepts_brick_capability_from_label_prefix()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "label-brick-peer",
            "https://label.example/",
            new Dictionary<string, string> { ["capability"] = "brick:label-brick" },
            Array.Empty<string>(),
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow));

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec(
            "label-brick",
            1,
            ["label-brick"],
            null,
            0,
            null));

        var (ok, placed, error) = await placement.TryScheduleAsync(created.TaskId);

        ok.Should().BeTrue(error);
        placed!.AssignedPeerId.Should().Be("label-brick-peer");
    }

    [Fact]
    public async Task TryScheduleAsync_overrides_correlation_id_when_provided()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);
        /// <summary>Register eligible node.</summary>
        await RegisterEligibleNode(nodes);

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec(
            "corr",
            1,
            Array.Empty<string>(),
            null,
            0,
            null,
            CorrelationId: "original-corr"));

        var (ok, placed, _) = await placement.TryScheduleAsync(created.TaskId, correlationId: "  override-corr  ");

        ok.Should().BeTrue();
        placed!.CorrelationId.Should().Be("override-corr");
    }

    [Fact]
    public async Task TryScheduleAsync_clamps_lease_seconds_override_to_minimum()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);
        /// <summary>Register eligible node.</summary>
        await RegisterEligibleNode(nodes);

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("lease", 1, Array.Empty<string>(), null, 0, null));
        var before = DateTimeOffset.UtcNow;

        var (ok, placed, _) = await placement.TryScheduleAsync(created.TaskId, leaseSecondsOverride: 10);

        ok.Should().BeTrue();
        placed!.LeaseExpiresUtc.Should().NotBeNull();
        placed.LeaseExpiresUtc!.Value.Should().BeOnOrAfter(before.AddSeconds(59));
        placed.LeaseExpiresUtc.Value.Should().BeOnOrBefore(before.AddSeconds(61));
    }

    [Fact]
    public async Task TryScheduleAsync_reclaims_expired_lease_before_replacement()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);
        /// <summary>Register eligible node.</summary>
        await RegisterEligibleNode(nodes);

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("reclaim", 1, Array.Empty<string>(), null, 0, null));
        await tasks.UpdateAsync(created with
        {
            Status = MeshTaskStatus.Assigned,
            AssignedPeerId = "eligible-peer",
            AssignedApiBaseUrl = "https://eligible.example/",
            LeaseToken = "old-token",
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
        });

        var (ok, placed, error) = await placement.TryScheduleAsync(created.TaskId);

        ok.Should().BeTrue(error);
        placed!.LeaseToken.Should().NotBe("old-token");
        placed.Status.Should().Be(MeshTaskStatus.Assigned);
    }

    [Fact]
    public async Task TryScheduleAsync_returns_existing_assignment_for_matching_idempotency_key()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);
        /// <summary>Register eligible node.</summary>
        await RegisterEligibleNode(nodes);

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("idem", 1, Array.Empty<string>(), null, 0, null));
        var (firstOk, firstPlaced, _) = await placement.TryScheduleAsync(created.TaskId, scheduleIdempotencyKey: "schedule-1");
        firstOk.Should().BeTrue();

        var (ok, placed, error) = await placement.TryScheduleAsync(created.TaskId, scheduleIdempotencyKey: "schedule-1");

        ok.Should().BeTrue(error);
        placed!.AssignedPeerId.Should().Be(firstPlaced!.AssignedPeerId);
        placed.LastScheduleIdempotencyKey.Should().Be("schedule-1");
    }

    [Fact]
    public async Task TryScheduleAsync_rejects_conflicting_idempotency_key_while_assigned()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);
        /// <summary>Register eligible node.</summary>
        await RegisterEligibleNode(nodes);

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("conflict", 1, Array.Empty<string>(), null, 0, null));
        (await placement.TryScheduleAsync(created.TaskId, scheduleIdempotencyKey: "first-key")).Ok.Should().BeTrue();

        var (ok, _, error) = await placement.TryScheduleAsync(created.TaskId, scheduleIdempotencyKey: "second-key");

        ok.Should().BeFalse();
        error.Should().Be("schedule.idempotency_conflict");
    }

    [Fact]
    public async Task TryScheduleAsync_reports_trust_policy_blocked_reason()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks, peerTrustPolicy: "trusted-only");

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "untrusted-peer",
            "https://untrusted.example/",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow,
            TrustTier: MeshFleetTrustTier.Untrusted));

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("trust", 1, Array.Empty<string>(), null, 0, null));
        var (ok, pending, error) = await placement.TryScheduleAsync(created.TaskId);

        ok.Should().BeFalse();
        error.Should().Be("placement.trust_policy_blocked");
        pending!.PlacementReason.Should().Be("placement.trust_policy_blocked");
    }

    [Fact]
    public async Task TryScheduleAsync_reports_peer_not_admitted_reason()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "pending-peer",
            "https://pending.example/",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow,
            Admitted: false));

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("admit", 1, Array.Empty<string>(), null, 0, null));
        var (ok, pending, error) = await placement.TryScheduleAsync(created.TaskId);

        ok.Should().BeFalse();
        error.Should().Be("placement.peer_not_admitted");
        pending!.PlacementReason.Should().Be("placement.peer_not_admitted");
    }

    [Fact]
    public async Task TryScheduleAsync_skips_drained_nodes()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "drained-peer",
            "https://drained.example/",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            Drained: true,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow));

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("drained", 1, Array.Empty<string>(), null, 0, null));
        var (ok, _, error) = await placement.TryScheduleAsync(created.TaskId);

        ok.Should().BeFalse();
        error.Should().Be("placement.no_eligible_nodes");
    }

    [Fact]
    public async Task TryScheduleAsync_requires_advertised_brick_capabilities()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "no-brick-peer",
            "https://nobrick.example/",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow));

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec(
            "needs-brick",
            1,
            ["required-brick"],
            null,
            0,
            null));

        var (ok, _, error) = await placement.TryScheduleAsync(created.TaskId);

        ok.Should().BeFalse();
        error.Should().Be("placement.no_eligible_nodes");
    }

    [Fact]
    public async Task TryRetryAsync_prefers_a_different_peer_when_available()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = CreatePlacement(nodes, tasks);

        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "peer-a",
            "https://a.example/",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow,
            ReportedQueueDepth: 0));
        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "peer-b",
            "https://b.example/",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow,
            ReportedQueueDepth: 0));

        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("retry", 1, Array.Empty<string>(), null, 0, null));
        var (firstOk, firstPlaced, _) = await placement.TryScheduleAsync(created.TaskId);
        firstOk.Should().BeTrue();

        var (ok, retried, error) = await placement.TryRetryAsync(created.TaskId);

        ok.Should().BeTrue(error);
        retried!.AssignedPeerId.Should().NotBe(firstPlaced!.AssignedPeerId);
    }

    private static MeshTaskPlacementService CreatePlacement(
        InMemoryFleetNodeRegistry nodes,
        InMemoryMeshTaskRegistry tasks,
        string peerTrustPolicy = "any")
        => new(
            nodes,
            tasks,
            Options.Create(new MeshCheckpointOptions { LeaseSeconds = 300 }),
            Options.Create(new MeshPlacementTrustOptions { PeerTrustPolicy = peerTrustPolicy }),
            NullLogger<MeshTaskPlacementService>.Instance);

    private static Task RegisterEligibleNode(InMemoryFleetNodeRegistry nodes)
        => nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "eligible-peer",
            "https://eligible.example",
            new Dictionary<string, string>(),
            Array.Empty<string>(),
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow));
}
