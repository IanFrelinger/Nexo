using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Tests for mesh task execution service gap coverage.</summary>
public sealed class MeshTaskExecutionServiceGapCoverageTests
{
    [Fact]
    public void Constructor_throws_for_null_dependencies()
    {
        var tasks = new InMemoryMeshTaskRegistry();
        var options = Options.Create(new MeshCheckpointOptions());

        var act = () => new MeshTaskExecutionService(null!, options);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tasks");

        act = () => new MeshTaskExecutionService(tasks, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("checkpointOptions");
    }

    [Fact]
    public async Task ExtendLeaseAsync_returns_not_found_for_missing_task()
    {
        var svc = CreateService(new InMemoryMeshTaskRegistry());

        var (ok, task, error) = await svc.ExtendLeaseAsync("missing", "token", extendSeconds: null);

        ok.Should().BeFalse();
        task.Should().BeNull();
        error.Should().Be("task.not_found");
    }

    [Fact]
    public async Task ExtendLeaseAsync_rejects_invalid_lease_token()
    {
        var tasks = new InMemoryMeshTaskRegistry();
        var svc = CreateService(tasks);
        var created = await SeedAssignedTask(tasks, leaseToken: "valid-token");

        var (ok, task, error) = await svc.ExtendLeaseAsync(created.TaskId, "wrong-token", extendSeconds: null);

        ok.Should().BeFalse();
        error.Should().Be("lease.invalid_token");
        task!.LeaseToken.Should().Be("valid-token");
    }

    [Fact]
    public async Task ExtendLeaseAsync_uses_checkpoint_default_when_extend_seconds_omitted()
    {
        var tasks = new InMemoryMeshTaskRegistry();
        var svc = new MeshTaskExecutionService(
            tasks,
            Options.Create(new MeshCheckpointOptions { LeaseSeconds = 300 }),
            NullLogger<MeshTaskExecutionService>.Instance);
        var created = await SeedAssignedTask(tasks, leaseToken: "tok");
        var before = DateTimeOffset.UtcNow;

        var (ok, next, error) = await svc.ExtendLeaseAsync(created.TaskId, "tok", extendSeconds: null);

        ok.Should().BeTrue(error);
        error.Should().BeNull();
        next!.LeaseExpiresUtc.Should().NotBeNull();
        next.LeaseExpiresUtc!.Value.Should().BeOnOrAfter(before.AddSeconds(299));
        next.LeaseExpiresUtc.Value.Should().BeOnOrBefore(before.AddSeconds(301));
    }

    [Fact]
    public async Task ExtendLeaseAsync_clamps_extend_seconds_to_minimum()
    {
        var tasks = new InMemoryMeshTaskRegistry();
        var svc = CreateService(tasks);
        var created = await SeedAssignedTask(tasks, leaseToken: "tok");
        var before = DateTimeOffset.UtcNow;

        var (ok, next, _) = await svc.ExtendLeaseAsync(created.TaskId, "tok", extendSeconds: 5);

        ok.Should().BeTrue();
        next!.LeaseExpiresUtc!.Value.Should().BeOnOrAfter(before.AddSeconds(59));
        next.LeaseExpiresUtc.Value.Should().BeOnOrBefore(before.AddSeconds(61));
    }

    [Fact]
    public async Task MigrateForCheckpointAsync_requires_checkpoint_handle()
    {
        var svc = CreateService(new InMemoryMeshTaskRegistry());

        var (ok, task, error) = await svc.MigrateForCheckpointAsync("any", "token", "   ");

        ok.Should().BeFalse();
        task.Should().BeNull();
        error.Should().Be("checkpoint.required");
    }

    [Fact]
    public async Task MigrateForCheckpointAsync_rejects_invalid_status()
    {
        var tasks = new InMemoryMeshTaskRegistry();
        var svc = CreateService(tasks);
        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("pending", 1, [], null, 0, null));
        await tasks.UpdateAsync(created with
        {
            LeaseToken = "tok",
            Status = MeshTaskStatus.Pending,
        });

        var (ok, task, error) = await svc.MigrateForCheckpointAsync(created.TaskId, "tok", "ckpt");

        ok.Should().BeFalse();
        error.Should().Be("migrate.invalid_status");
        task!.Status.Should().Be(MeshTaskStatus.Pending);
    }

    [Fact]
    public async Task MigrateForCheckpointAsync_accepts_assigned_status_and_trims_handle()
    {
        var tasks = new InMemoryMeshTaskRegistry();
        var svc = CreateService(tasks);
        var created = await SeedAssignedTask(tasks, leaseToken: "tok");

        var (ok, next, error) = await svc.MigrateForCheckpointAsync(created.TaskId, "tok", "  ckpt-trimmed  ");

        ok.Should().BeTrue(error);
        error.Should().BeNull();
        next!.CheckpointHandle.Should().Be("ckpt-trimmed");
        next.PlacementReason.Should().Be("migrate.checkpoint_ready");
        next.Status.Should().Be(MeshTaskStatus.Pending);
    }

    [Fact]
    public async Task ExtendLeaseAsync_accepts_trimmed_matching_token()
    {
        var tasks = new InMemoryMeshTaskRegistry();
        var svc = CreateService(tasks);
        var created = await SeedAssignedTask(tasks, leaseToken: "trim-token");

        var (ok, _, error) = await svc.ExtendLeaseAsync(created.TaskId, "  trim-token  ", 120);

        ok.Should().BeTrue(error);
        error.Should().BeNull();
    }

    private static MeshTaskExecutionService CreateService(InMemoryMeshTaskRegistry tasks)
        => new(
            tasks,
            Options.Create(new MeshCheckpointOptions { LeaseSeconds = 120 }),
            NullLogger<MeshTaskExecutionService>.Instance);

    private static async Task<MeshTaskState> SeedAssignedTask(InMemoryMeshTaskRegistry tasks, string leaseToken)
    {
        var created = await tasks.CreateAsync(new MeshTaskCreateSpec("job", 1, [], null, 0, null));
        var assigned = created with
        {
            Status = MeshTaskStatus.Assigned,
            AssignedPeerId = "peer-1",
            LeaseToken = leaseToken,
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(1),
        };
        await tasks.UpdateAsync(assigned);
        return assigned;
    }
}
