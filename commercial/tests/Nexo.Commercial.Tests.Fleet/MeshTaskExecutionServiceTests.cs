using FluentAssertions;
using Microsoft.Extensions.Options;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Contracts.Ports;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Tests for mesh task execution service.</summary>
public sealed class MeshTaskExecutionServiceTests
{
    /// <summary>Creates service.</summary>
    /// <param name="tasks">Tasks.</param>
    private static MeshTaskExecutionService CreateService(InMemoryMeshTaskRegistry tasks) =>
        new(tasks, Options.Create(new MeshCheckpointOptions { LeaseSeconds = 120 }));

    [Fact]
    public async Task ExtendLeaseAsync_updates_expiry_when_token_matches()
    {
        var tasks = new InMemoryMeshTaskRegistry();
        var svc = CreateService(tasks);
        var created = await tasks.CreateAsync(new MeshTaskCreateSpec(null, 1, new[] { "b" }, null, 0, null));
        var withLease = created with
        {
            Status = MeshTaskStatus.Assigned,
            AssignedPeerId = "p",
            LeaseToken = "tok",
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(1)
        };
        await tasks.UpdateAsync(withLease);

        var (ok, next, err) = await svc.ExtendLeaseAsync(created.TaskId, "tok", 120);
        ok.Should().BeTrue(err);
        err.Should().BeNull();
        next!.LeaseExpiresUtc.Should().NotBeNull();
        next.LeaseExpiresUtc!.Value.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task MigrateForCheckpointAsync_clears_assignment_and_sets_checkpoint()
    {
        var tasks = new InMemoryMeshTaskRegistry();
        var svc = CreateService(tasks);
        var created = await tasks.CreateAsync(new MeshTaskCreateSpec(null, 1, new[] { "b" }, null, 0, null));
        var running = created with
        {
            Status = MeshTaskStatus.Running,
            AssignedPeerId = "p",
            LeaseToken = "tok",
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
        };
        await tasks.UpdateAsync(running);

        var (ok, next, err) = await svc.MigrateForCheckpointAsync(created.TaskId, "tok", "ckpt-1");
        ok.Should().BeTrue(err);
        next!.Status.Should().Be(MeshTaskStatus.Pending);
        next.CheckpointHandle.Should().Be("ckpt-1");
        next.AssignedPeerId.Should().BeNull();
        next.LeaseToken.Should().BeNull();
    }
}
