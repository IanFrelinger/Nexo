using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Ashlar.Abstractions.Transport;
using Ashlar.Runtime.Transport;
using Xunit;

namespace Ashlar.Tests.Transport;

/// <summary>Tests for routing agent transport.</summary>
public sealed class RoutingAgentTransportTests
{
    [Fact]
    public async Task SendAsync_WithNullTargetEndpoint_UsesInProcessTransport()
    {
        var inProcess = new Mock<IAgentTransport>(MockBehavior.Strict);
        var remote = new Mock<IAgentTransport>(MockBehavior.Strict);

        inProcess
            .Setup(t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult(true));

        var sut = new RoutingAgentTransport(inProcess.Object, remote.Object, NullLogger<RoutingAgentTransport>.Instance);
        var request = new AgentInvocationRequest(
            AgentName: "agent-a",
            CorrelationId: "corr",
            Options: new AgentInvocationOptions(TimeSpan.FromSeconds(1), 1, null));

        var result = await sut.SendAsync(request);

        result.Success.Should().BeTrue();
        inProcess.Verify(t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        remote.Verify(t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WithTargetEndpoint_UsesRemoteTransport()
    {
        var inProcess = new Mock<IAgentTransport>(MockBehavior.Strict);
        var remote = new Mock<IAgentTransport>(MockBehavior.Strict);

        remote
            .Setup(t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentResult(true));

        var sut = new RoutingAgentTransport(inProcess.Object, remote.Object, NullLogger<RoutingAgentTransport>.Instance);
        var request = new AgentInvocationRequest(
            AgentName: "agent-a",
            CorrelationId: "corr",
            Options: new AgentInvocationOptions(TimeSpan.FromSeconds(1), 1, "http://127.0.0.1:5100"));

        var result = await sut.SendAsync(request);

        result.Success.Should().BeTrue();
        remote.Verify(t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        inProcess.Verify(t => t.SendAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckHealthAsync_AggregatesBothTransportResults()
    {
        var inProcess = new Mock<IAgentTransport>(MockBehavior.Strict);
        var remote = new Mock<IAgentTransport>(MockBehavior.Strict);

        inProcess
            .Setup(t => t.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransportHealth(true, "in-process", "ok"));
        remote
            .Setup(t => t.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransportHealth(false, "gRPC", "down"));

        var sut = new RoutingAgentTransport(inProcess.Object, remote.Object, NullLogger<RoutingAgentTransport>.Instance);
        var health = await sut.CheckHealthAsync();

        health.IsHealthy.Should().BeFalse();
        health.TransportType.Should().Be("routing");
        health.DiagnosticMessage.Should().Contain("in-process");
        health.DiagnosticMessage.Should().Contain("remote");
    }
}
