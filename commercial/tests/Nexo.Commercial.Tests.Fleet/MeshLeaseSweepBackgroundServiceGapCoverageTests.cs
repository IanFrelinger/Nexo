using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Adaptation.Ports;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Contracts.Ports;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Tests for mesh lease sweep background service gap coverage.</summary>
public sealed class MeshLeaseSweepBackgroundServiceGapCoverageTests
{
    [Fact]
    public async Task ExecuteAsync_skips_when_sweep_disabled()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var created = await registry.CreateAsync(new MeshTaskCreateSpec("job", 1, [], null, 0, null));
        await registry.UpdateAsync(created with
        {
            Status = MeshTaskStatus.Assigned,
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
        });

        var service = new MeshLeaseSweepBackgroundService(
            registry,
            new StaticOptionsMonitor<MeshCheckpointOptions>(new MeshCheckpointOptions { SweepEnabled = false }),
            NullLogger<MeshLeaseSweepBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        (await registry.GetAsync(created.TaskId))!.Status.Should().Be(MeshTaskStatus.Assigned);
    }

    [Fact]
    public async Task ExecuteAsync_reclaims_expired_running_tasks()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var created = await registry.CreateAsync(new MeshTaskCreateSpec("running-job", 1, [], null, 0, null));
        await registry.UpdateAsync(created with
        {
            Status = MeshTaskStatus.Running,
            AssignedPeerId = "peer-1",
            AssignedApiBaseUrl = "http://peer:8080",
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
        });

        var service = new MeshLeaseSweepBackgroundService(
            registry,
            new StaticOptionsMonitor<MeshCheckpointOptions>(new MeshCheckpointOptions
            {
                SweepEnabled = true,
                SweepIntervalMinutes = 60,
            }),
            NullLogger<MeshLeaseSweepBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(250);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        var updated = await registry.GetAsync(created.TaskId);
        updated!.Status.Should().Be(MeshTaskStatus.Pending);
        updated.PlacementReason.Should().Be("lease.expired");
    }

    [Fact]
    public async Task ExecuteAsync_leaves_non_expired_leases_unchanged()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var created = await registry.CreateAsync(new MeshTaskCreateSpec("active-job", 1, [], null, 0, null));
        await registry.UpdateAsync(created with
        {
            Status = MeshTaskStatus.Assigned,
            LeaseExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
        });

        var service = new MeshLeaseSweepBackgroundService(
            registry,
            new StaticOptionsMonitor<MeshCheckpointOptions>(new MeshCheckpointOptions
            {
                SweepEnabled = true,
                SweepIntervalMinutes = 60,
            }),
            NullLogger<MeshLeaseSweepBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        (await registry.GetAsync(created.TaskId))!.Status.Should().Be(MeshTaskStatus.Assigned);
    }
}
