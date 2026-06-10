using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nexo.Commercial.Fleet.Infrastructure.Communication;
using Nexo.Core.Application.Networking.Models;
using Nexo.Core.Application.Networking.Ports;
using Nexo.Orchestration.Communication;
using Nexo.Orchestration.Communication.Models;
using Xunit;

namespace Nexo.Commercial.Tests.Fleet.Communication;

public sealed class AgentBusNetworkBridgeTests
{
    [Fact]
    public async Task AgentBusNetworkBridge_forwards_local_messages_and_delivers_network_events()
    {
        var agentBus = new AgentBus(NullLogger<AgentBus>.Instance);
        var deliveredTcs = new TaskCompletionSource<AgentMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        await agentBus.SubscribeAsync(null, (msg, _) =>
        {
            if (msg is ForwardedAgentMessage)
                deliveredTcs.TrySetResult(msg);
            return Task.CompletedTask;
        });

        var broadcastedTcs = new TaskCompletionSource<NetworkEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<NetworkEvent, CancellationToken, Task>? networkHandler = null;

        var networkBus = new Mock<INetworkBus>();
        networkBus.Setup(n => n.NodeId).Returns("node-test");
        networkBus.Setup(n => n.SubscribeAsync(
                NetworkEventTypes.AgentMessage,
                It.IsAny<Func<NetworkEvent, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string?, Func<NetworkEvent, CancellationToken, Task>, CancellationToken>((_, h, _) => networkHandler = h)
            .ReturnsAsync(Mock.Of<IDisposable>());
        networkBus.Setup(n => n.BroadcastAsync(It.IsAny<NetworkEvent>(), It.IsAny<CancellationToken>()))
            .Callback<NetworkEvent, CancellationToken>((evt, _) => broadcastedTcs.TrySetResult(evt))
            .Returns(Task.CompletedTask);

        var bridge = new AgentBusNetworkBridge(
            agentBus,
            networkBus.Object,
            Options.Create(new AgentBusNetworkBridgeOptions { ForwardAgentMessagesToNetwork = true }),
            NullLogger<AgentBusNetworkBridge>.Instance);

        await bridge.StartAsync(CancellationToken.None);

        await agentBus.PublishAsync(new OutputEmitted
        {
            MessageId = "local-1",
            FromAgentId = "agent-a",
            MessageType = "OutputEmitted",
            Output = new { ok = true },
        });

        var broadcasted = await broadcastedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        broadcasted.EventType.Should().Be(NetworkEventTypes.AgentMessage);

        var wirePayload = JsonSerializer.SerializeToElement(new
        {
            messageId = "net-1",
            fromAgentId = "agent-b",
            messageType = "OutputEmitted",
            timestamp = DateTimeOffset.UtcNow,
            payload = JsonDocument.Parse("""{"x":1}""").RootElement,
        });
        var networkEvt = new NetworkEvent
        {
            EventId = "evt-1",
            SourceNodeId = "remote-node",
            EventType = NetworkEventTypes.AgentMessage,
            Timestamp = DateTimeOffset.UtcNow,
            Payload = wirePayload,
        };

        await networkHandler!(networkEvt, CancellationToken.None);
        var delivered = await deliveredTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        delivered.Should().BeOfType<ForwardedAgentMessage>();

        await bridge.StopAsync(CancellationToken.None);
        bridge.Dispose();
    }

    [Fact]
    public void AgentBusNetworkBridgeOptions_has_defaults()
    {
        var options = new AgentBusNetworkBridgeOptions();
        options.ForwardAgentMessagesToNetwork.Should().BeTrue();
        options.MaxHops.Should().Be(3);
        AgentBusNetworkBridgeOptions.SectionName.Should().Be("AgentBusNetworkBridge");
    }
}
