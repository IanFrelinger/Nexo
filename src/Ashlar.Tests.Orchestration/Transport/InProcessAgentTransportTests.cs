using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Abstractions.Transport;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Transport;
using Xunit;

namespace Ashlar.Tests.Orchestration.Transport;

/// <summary>Tests for in process agent transport.</summary>
public sealed class InProcessAgentTransportTests
{
    [Fact]
    public async Task SendAsync_WithRegisteredAgent_ReturnsSuccessfulResult()
    {
        // Arrange
        var lifecycleManager = CreateLifecycleManager();
        var agent = new GenericAgent(
            new AgentSpawnSpec
            {
                AgentId = "agent-1",
                Domain = "General",
                Goal = "Generate output"
            },
            NullLogger<GenericAgent>.Instance);

        var container = new AgentContainer(agent, NullLogger<AgentContainer>.Instance);
        await lifecycleManager.RegisterAgentAsync(container);

        var transport = new InProcessAgentTransport(
            lifecycleManager,
            NullLogger<InProcessAgentTransport>.Instance);

        // Act
        var result = await transport.SendAsync(
            new AgentInvocationRequest("agent-1", "corr-1"));

        // Assert
        result.Success.Should().BeTrue();
        result.Output.Should().NotBeNull();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WithUnknownAgent_ReturnsFailureResult()
    {
        // Arrange
        var lifecycleManager = CreateLifecycleManager();
        var transport = new InProcessAgentTransport(
            lifecycleManager,
            NullLogger<InProcessAgentTransport>.Instance);

        // Act
        var result = await transport.SendAsync(
            new AgentInvocationRequest("missing-agent", "corr-2"));

        // Assert
        result.Success.Should().BeFalse();
        result.Output.Should().BeNull();
        result.ErrorMessage.Should().Contain("not registered");
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthyChannel()
    {
        // Arrange
        var lifecycleManager = CreateLifecycleManager();
        var transport = new InProcessAgentTransport(
            lifecycleManager,
            NullLogger<InProcessAgentTransport>.Instance);

        // Act
        var health = await transport.CheckHealthAsync();

        // Assert
        health.IsHealthy.Should().BeTrue();
        health.TransportName.Should().Be(nameof(InProcessAgentTransport));
    }

    private static LifecycleManager CreateLifecycleManager()
    {
        return new LifecycleManager(
            NullLogger<LifecycleManager>.Instance,
            new HealthMonitor(NullLogger<HealthMonitor>.Instance));
    }
}
