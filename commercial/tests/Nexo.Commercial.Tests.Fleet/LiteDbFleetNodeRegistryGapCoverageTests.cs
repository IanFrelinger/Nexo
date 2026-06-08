using FluentAssertions;
using Nexo.Commercial.Fleet.Contracts.Models;
using Nexo.Commercial.Fleet.Infrastructure;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet;

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

public sealed class MeshPersistenceOptionsGapCoverageTests
{
    [Fact]
    public void Defaults_match_expected_mesh_persistence_configuration()
    {
        var options = new MeshPersistenceOptions();

        MeshPersistenceOptions.SectionPath.Should().Be("Nexo:Mesh:Persistence");
        options.Provider.Should().Be("InMemory");
        options.DatabasePath.Should().Be("mesh-director.db");
    }

    [Theory]
    [InlineData("LiteDb", true)]
    [InlineData("litedb", true)]
    [InlineData("InMemory", false)]
    [InlineData("unknown", false)]
    public void IsLiteDb_recognizes_provider_names(string provider, bool expected)
    {
        MeshPersistenceOptions.IsLiteDb(provider).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("InMemory", true)]
    [InlineData("LiteDb", true)]
    [InlineData("postgres", false)]
    public void IsKnownProvider_accepts_supported_values(string? provider, bool expected)
    {
        MeshPersistenceOptions.IsKnownProvider(provider).Should().Be(expected);
    }
}
