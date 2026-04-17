using FluentAssertions;
using Nexo.Core.Domain;
using Nexo.Infrastructure.Networking;
using Nexo.Tests.Infrastructure.Helpers;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Networking;

public sealed class NetworkBusOptionsTests
{
    [Fact(Timeout = TestTimeouts.Quick)]
    public void DefaultValues_MatchNexoDefaults()
    {
        var opts = new NetworkBusOptions();

        opts.HeartbeatIntervalSeconds.Should().Be(NexoDefaults.NetworkBusHeartbeatIntervalSeconds);
        opts.MaxEventHistory.Should().Be(NexoDefaults.NetworkBusMaxEventHistory);
        opts.DefaultMaxHops.Should().Be(NexoDefaults.NetworkBusDefaultMaxHops);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void DefaultNodeId_IsMachineName()
    {
        var opts = new NetworkBusOptions();

        opts.NodeId.Should().Be(Environment.MachineName);
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void DefaultPeerUrls_IsEmpty()
    {
        var opts = new NetworkBusOptions();

        opts.PeerUrls.Should().BeEmpty();
    }

    [Fact(Timeout = TestTimeouts.Quick)]
    public void CustomValues_OverrideDefaults()
    {
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

    [Fact(Timeout = TestTimeouts.Quick)]
    public void SectionName_IsNetworkBus()
    {
        NetworkBusOptions.SectionName.Should().Be("NetworkBus");
    }
}
