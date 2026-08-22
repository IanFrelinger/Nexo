using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Contracts.Ports;
using Ashlar.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet;

/// <summary>Tests for mesh elastic placement.</summary>
public sealed class MeshElasticPlacementTests
{
    [Fact]
    public async Task Schedule_prefers_lower_reported_queue_depth()
    {
        var nodes = new InMemoryFleetNodeRegistry();
        var tasks = new InMemoryMeshTaskRegistry();
        var placement = new MeshTaskPlacementService(
            nodes,
            tasks,
            Options.Create(new MeshCheckpointOptions()),
            Options.Create(new MeshPlacementTrustOptions()),
            NullLogger<MeshTaskPlacementService>.Instance);

        var t = DateTimeOffset.UtcNow;
        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "busy", "https://busy/", new Dictionary<string, string>(), new[] { "b" },
            false, t, t, ReportedQueueDepth: 10));
        await nodes.RegisterOrUpdateAsync(new MeshFleetNodeState(
            "idle", "https://idle/", new Dictionary<string, string>(), new[] { "b" },
            false, t, t, ReportedQueueDepth: 0));

        var task = await tasks.CreateAsync(new MeshTaskCreateSpec(null, 1, new[] { "b" }, null, 0, null));
        var (ok, placed, _) = await placement.TryScheduleAsync(task.TaskId);
        ok.Should().BeTrue();
        placed!.AssignedPeerId.Should().Be("idle");
    }
}
