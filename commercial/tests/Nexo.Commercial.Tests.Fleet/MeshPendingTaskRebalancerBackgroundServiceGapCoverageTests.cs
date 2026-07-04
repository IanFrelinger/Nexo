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

/// <summary>Tests for mesh pending task rebalancer background service gap coverage.</summary>
public sealed class MeshPendingTaskRebalancerBackgroundServiceGapCoverageTests
{
    [Fact]
    public async Task ExecuteAsync_skips_when_disabled()
    {
        var registry = new InMemoryMeshTaskRegistry();
        var created = await registry.CreateAsync(new MeshTaskCreateSpec("stale", 1, [], null, 0, null));
        await registry.UpdateAsync(created with
        {
            Status = MeshTaskStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
        });

        var placement = new Mock<IMeshTaskPlacementService>();
        var service = new MeshPendingTaskRebalancerBackgroundService(
            registry,
            placement.Object,
            new StaticOptionsMonitor<MeshElasticSchedulingOptions>(new MeshElasticSchedulingOptions { Enabled = false }),
            NullLogger<MeshPendingTaskRebalancerBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        placement.Verify(
            p => p.TryScheduleAsync(It.IsAny<string>(), null, null, null, It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
