using FluentAssertions;
using Nexo.Core.Application.Fleet.Models;
using Nexo.Infrastructure.Fleet;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Fleet;

public sealed class InMemoryFleetNodeRegistryGapCoverageTests
{
    [Fact]
    public async Task GetAsync_returns_null_for_missing_peer()
    {
        var registry = new InMemoryFleetNodeRegistry();

        (await registry.GetAsync("missing")).Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_returns_false_for_missing_peer()
    {
        var registry = new InMemoryFleetNodeRegistry();

        (await registry.RemoveAsync("missing")).Should().BeFalse();
    }

    [Fact]
    public async Task RegisterOrUpdateAsync_overwrites_existing_peer()
    {
        var registry = new InMemoryFleetNodeRegistry();
        await registry.RegisterOrUpdateAsync(SampleNode("peer-a", queueDepth: 1));
        await registry.RegisterOrUpdateAsync(SampleNode("peer-a", queueDepth: 9));

        (await registry.GetAsync("peer-a"))!.ReportedQueueDepth.Should().Be(9);
    }

    [Fact]
    public async Task ListAsync_orders_peers_case_insensitively()
    {
        var registry = new InMemoryFleetNodeRegistry();
        await registry.RegisterOrUpdateAsync(SampleNode("Beta"));
        await registry.RegisterOrUpdateAsync(SampleNode("alpha"));

        var peers = await registry.ListAsync();

        peers.Select(p => p.PeerId).Should().ContainInOrder("alpha", "Beta");
    }

    [Fact]
    public async Task HeartbeatAsync_clamps_negative_queue_depth_to_zero()
    {
        var registry = new InMemoryFleetNodeRegistry();
        await registry.RegisterOrUpdateAsync(SampleNode("peer-a", queueDepth: 3));

        await registry.HeartbeatAsync("peer-a", reportedQueueDepth: -5);

        (await registry.GetAsync("peer-a"))!.ReportedQueueDepth.Should().Be(0);
    }

    [Fact]
    public async Task HeartbeatAsync_is_no_op_for_unknown_peer()
    {
        var registry = new InMemoryFleetNodeRegistry();

        var act = async () => await registry.HeartbeatAsync("missing", reportedQueueDepth: 1);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetDrained_and_SetAdmitted_mutate_existing_peer()
    {
        var registry = new InMemoryFleetNodeRegistry();
        await registry.RegisterOrUpdateAsync(SampleNode("peer-a"));

        (await registry.SetDrainedAsync("peer-a", true)).Should().BeTrue();
        (await registry.SetAdmittedAsync("peer-a", false)).Should().BeTrue();

        var node = await registry.GetAsync("peer-a");
        node!.Drained.Should().BeTrue();
        node.Admitted.Should().BeFalse();
    }

    private static MeshFleetNodeState SampleNode(string peerId, int queueDepth = 0)
        => new(
            PeerId: peerId,
            ApiBaseUrl: $"https://{peerId}.example/",
            Labels: new Dictionary<string, string>(),
            AdvertisedBrickIds: Array.Empty<string>(),
            Drained: false,
            LastHeartbeatUtc: DateTimeOffset.UtcNow,
            RegisteredAtUtc: DateTimeOffset.UtcNow,
            ReportedQueueDepth: queueDepth);
}
