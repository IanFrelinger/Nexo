using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Orchestration.Communication;
using Nexo.Orchestration.Communication.Models;
using Xunit;

namespace Nexo.Tests.Orchestration.Communication;

public class AgentBusTests
{
    private readonly Mock<ILogger<AgentBus>> _loggerMock;
    private readonly AgentBus _bus;

    public AgentBusTests()
    {
        _loggerMock = new Mock<ILogger<AgentBus>>();
        _bus = new AgentBus(_loggerMock.Object);
    }

    [Fact]
    public async Task PublishAsync_MessagePublished_SubscribersReceiveIt()
    {
        // Arrange
        var messageReceived = false;
        var message = new OutputEmitted
        {
            MessageId = "msg-1",
            FromAgentId = "agent-1",
            MessageType = "OutputEmitted",
            Output = new { Result = "test" }
        };

        // Act
        await _bus.SubscribeAsync("OutputEmitted", async (msg, ct) =>
        {
            messageReceived = true;
            await Task.CompletedTask;
        });

        await _bus.PublishAsync(message);

        // Assert
        // Give async handlers time to execute
        await Task.Delay(100);
        messageReceived.Should().BeTrue();
    }

    [Fact]
    public async Task SubscribeAsync_WithAgentIdFilter_OnlyReceivesMatchingMessages()
    {
        // Arrange
        var receivedMessages = new List<AgentMessage>();
        var message1 = new OutputEmitted
        {
            MessageId = "msg-1",
            FromAgentId = "agent-1",
            ToAgentId = "agent-2",
            MessageType = "OutputEmitted",
            Output = new { Result = "test" }
        };

        var message2 = new OutputEmitted
        {
            MessageId = "msg-2",
            FromAgentId = "agent-3",
            ToAgentId = "agent-4",
            MessageType = "OutputEmitted",
            Output = new { Result = "test2" }
        };

        // Act
        await _bus.SubscribeAsync("OutputEmitted", async (msg, ct) =>
        {
            receivedMessages.Add(msg);
            await Task.CompletedTask;
        }, "agent-2");

        await _bus.PublishAsync(message1);
        await _bus.PublishAsync(message2);

        // Assert
        await Task.Delay(100);
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].MessageId.Should().Be("msg-1");
    }

    [Fact]
    public async Task GetMessagesForAgentAsync_ReturnsMessagesForAgent()
    {
        // Arrange
        var message = new OutputEmitted
        {
            MessageId = "msg-1",
            FromAgentId = "agent-1",
            ToAgentId = "agent-2",
            MessageType = "OutputEmitted",
            Output = new { Result = "test" }
        };

        await _bus.PublishAsync(message);

        // Act
        var messages = await _bus.GetMessagesForAgentAsync("agent-2");

        // Assert
        messages.Should().Contain(m => m.MessageId == "msg-1");
    }
}

