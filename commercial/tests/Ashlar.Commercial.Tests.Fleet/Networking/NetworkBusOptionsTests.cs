using FluentAssertions;
using Ashlar.Core.Domain;
using Ashlar.Commercial.Fleet.Infrastructure.Networking;
using Xunit;

namespace Ashlar.Commercial.Tests.Fleet.Networking;

/// <summary>Tests for network bus options.</summary>
public sealed class NetworkBusOptionsTests
{
    [Fact]
    public async Task DefaultValues_MatchAshlarDefaults()
    {
        await Task.CompletedTask;
        var opts = new NetworkBusOptions();

        opts.HeartbeatIntervalSeconds.Should().Be(AshlarDefaults.NetworkBusHeartbeatIntervalSeconds);
        opts.MaxEventHistory.Should().Be(AshlarDefaults.NetworkBusMaxEventHistory);
        opts.DefaultMaxHops.Should().Be(AshlarDefaults.NetworkBusDefaultMaxHops);
    }

    [Fact]
    public async Task DefaultNodeId_IsMachineName()
    {
        await Task.CompletedTask;
        var opts = new NetworkBusOptions();

        opts.NodeId.Should().Be(Environment.MachineName);
    }

    [Fact]
    public async Task DefaultPeerUrls_IsEmpty()
    {
        await Task.CompletedTask;
        var opts = new NetworkBusOptions();

        opts.PeerUrls.Should().BeEmpty();
    }

    [Fact]
    public async Task CustomValues_OverrideDefaults()
    {
        await Task.CompletedTask;
        var opts = new NetworkBusOptions
        {
            NodeId = "custom-node",
            HeartbeatIntervalSeconds = 60,
            MaxEventHistory = 5_000,
            DefaultMaxHops = 5,
            PeerUrls = new[] { "https://peer-a.example.com", "https://peer-b.example.com" }
        };

        opts.NodeId.Should().Be("custom-node");
        opts.HeartbeatIntervalSeconds.Should().Be(60);
        opts.MaxEventHistory.Should().Be(5_000);
        opts.DefaultMaxHops.Should().Be(5);
        opts.PeerUrls.Should().HaveCount(2);
    }

    [Fact]
    public async Task SectionName_IsNetworkBus()
    {
        await Task.CompletedTask;
        NetworkBusOptions.SectionName.Should().Be("NetworkBus");
    }
}
