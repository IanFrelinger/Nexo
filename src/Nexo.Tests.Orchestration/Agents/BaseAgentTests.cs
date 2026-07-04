using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Orchestration.Agents;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Architect.Models;
using Xunit;

namespace Nexo.Tests.Orchestration.Agents;

/// <summary>Tests for base agent.</summary>
public class BaseAgentTests
{
    private readonly Mock<ILogger<GenericAgent>> _loggerMock;

    public BaseAgentTests()
    {
        _loggerMock = new Mock<ILogger<GenericAgent>>();
    }

    [Fact]
    public async Task InitializeAsync_ChangesStateToReady()
    {
        // Arrange
        var spec = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system"
        };

        var agent = new GenericAgent(spec, _loggerMock.Object);

        // Act
        await agent.InitializeAsync();

        // Assert
        agent.State.Should().Be(AgentState.Ready);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReady_ReturnsOutput()
    {
        // Arrange
        var spec = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system"
        };

        var agent = new GenericAgent(spec, _loggerMock.Object);
        await agent.InitializeAsync();

        // Act
        var result = await agent.ExecuteAsync();

        // Assert
        result.Should().NotBeNull();
        agent.State.Should().Be(AgentState.Completed);
        agent.Output.Should().NotBeNull();
    }

    [Fact]
    public async Task WaitForDependenciesAsync_WhenDependenciesProvided_ChangesStateToReady()
    {
        // Arrange
        var spec = new AgentSpawnSpec
        {
            AgentId = "agent-2",
            Domain = "Economy",
            Goal = "Design economy",
            Dependencies = new[] { "agent-1" }
        };

        var agent = new GenericAgent(spec, _loggerMock.Object);
        await agent.InitializeAsync();
        // State will be set to WaitingForDependencies when we call WaitForDependenciesAsync

        var dependencyOutputs = new Dictionary<string, object>
        {
            ["agent-1"] = new { Result = "dependency output" }
        };

        // Act
        await agent.WaitForDependenciesAsync(dependencyOutputs);

        // Assert
        agent.State.Should().Be(AgentState.Ready);
    }

    [Fact]
    public async Task ShutdownAsync_ChangesStateToTerminated()
    {
        // Arrange
        var spec = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system"
        };

        var agent = new GenericAgent(spec, _loggerMock.Object);
        await agent.InitializeAsync();

        // Act
        await agent.ShutdownAsync();

        // Assert
        agent.State.Should().Be(AgentState.Terminated);
    }
}

