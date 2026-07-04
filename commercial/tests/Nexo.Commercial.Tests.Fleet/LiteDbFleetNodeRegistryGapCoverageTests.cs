using FluentAssertions;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

/// <summary>Tests for lite db fleet node registry gap coverage.</summary>
[Collection(nameof(LiteDbFleetCollection))]
public sealed class LiteDbFleetNodeRegistryGapCoverageTests
{
    [Fact]
    public async Task ListAsync_orders_peers_case_insensitively()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbFleetNodeRegistry(path);
            await registry.RegisterOrUpdateAsync(SampleNode("Zulu"));
            await registry.RegisterOrUpdateAsync(SampleNode("alpha"));

            var peers = await registry.ListAsync();

            peers.Select(p => p.PeerId).Should().ContainInOrder("alpha", "Zulu");
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    [Fact]
    public async Task HeartbeatAsync_clamps_negative_queue_depth_to_zero()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbFleetNodeRegistry(path);
            await registry.RegisterOrUpdateAsync(SampleNode("peer-a", queueDepth: 4));

            await registry.HeartbeatAsync("peer-a", reportedQueueDepth: -3);

            (await registry.GetAsync("peer-a"))!.ReportedQueueDepth.Should().Be(0);
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    [Fact]
    public async Task HeartbeatAsync_preserves_existing_depth_when_reported_depth_is_null()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbFleetNodeRegistry(path);
            await registry.RegisterOrUpdateAsync(SampleNode("peer-a", queueDepth: 7));

            await registry.HeartbeatAsync("peer-a");

            (await registry.GetAsync("peer-a"))!.ReportedQueueDepth.Should().Be(7);
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    [Fact]
    public async Task RegisterOrUpdateAsync_overwrites_existing_peer()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbFleetNodeRegistry(path);
            await registry.RegisterOrUpdateAsync(SampleNode("peer-a", queueDepth: 1));
            await registry.RegisterOrUpdateAsync(SampleNode("peer-a", queueDepth: 11));

            (await registry.GetAsync("peer-a"))!.ReportedQueueDepth.Should().Be(11);
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
    }

    [Fact]
    public void Accepts_filename_connection_string()
    {
        var path = CreateTempDbPath();
        try
        {
            var registry = new LiteDbFleetNodeRegistry($"Filename={path}");

            registry.Should().NotBeNull();
        }
        finally
        {
            /// <summary>Attempts to delete; returns false on failure.</summary>
            TryDelete(path);
        }
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

    private static string CreateTempDbPath()
        => Path.Combine(Path.GetTempPath(), "nexo-lite-fleet-" + Guid.NewGuid().ToString("N") + ".db");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort temp cleanup
        }
    }
}
