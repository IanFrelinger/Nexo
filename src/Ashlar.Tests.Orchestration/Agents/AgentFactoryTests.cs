using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Agents.Templates;
using Ashlar.Orchestration.Architect.Models;
using Xunit;

namespace Ashlar.Tests.Orchestration.Agents;

/// <summary>Tests for agent factory.</summary>
public class AgentFactoryTests
{
    private readonly IServiceProvider _serviceProvider;

    public AgentFactoryTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void CreateAgent_CombatDomain_ReturnsCombatAgent()
    {
        // Arrange
        var spec = new AgentSpawnSpec
        {
            AgentId = "combat-1",
            Domain = "Combat",
            Goal = "Design weapon system"
        };

        var logger = _serviceProvider.GetRequiredService<ILogger<AgentFactory>>();
        var factory = new AgentFactory(logger, _serviceProvider);

        // Act
        var agent = factory.CreateAgent(spec);

        // Assert
        agent.Should().BeOfType<CombatAgent>();
        agent.Name.Should().Be("combat-1");
    }

    [Fact]
    public void CreateAgent_EconomyDomain_ReturnsEconomyAgent()
    {
        // Arrange
        var spec = new AgentSpawnSpec
        {
            AgentId = "economy-1",
            Domain = "Economy",
            Goal = "Design economy system"
        };

        var logger = _serviceProvider.GetRequiredService<ILogger<AgentFactory>>();
        var factory = new AgentFactory(logger, _serviceProvider);

        // Act
        var agent = factory.CreateAgent(spec);

        // Assert
        agent.Should().BeOfType<EconomyAgent>();
    }

    [Fact]
    public void CreateContainer_WrapsAgentInContainer()
    {
        // Arrange
        var spec = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Gameplay",
            Goal = "Design gameplay system"
        };

        var logger = _serviceProvider.GetRequiredService<ILogger<AgentFactory>>();
        var factory = new AgentFactory(logger, _serviceProvider);

        // Act
        var container = factory.CreateContainer(spec);

        // Assert
        container.Should().NotBeNull();
        container.AgentId.Should().Be("agent-1");
        container.Agent.Should().NotBeNull();
    }
}

