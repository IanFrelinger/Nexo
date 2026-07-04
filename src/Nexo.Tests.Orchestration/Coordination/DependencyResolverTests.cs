using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Coordination;
using Xunit;

namespace Nexo.Tests.Orchestration.Coordination;

/// <summary>Tests for dependency resolver.</summary>
public class DependencyResolverTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DependencyResolver> _logger;

    public DependencyResolverTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<DependencyResolver>>();
    }

    [Fact]
    public async Task GetReadyAgents_WhenDependenciesResolved_ReturnsAgents()
    {
        // Arrange
        var resolver = new DependencyResolver(_logger);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var spec1 = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system",
            Dependencies = Array.Empty<string>()
        };

        var spec2 = new AgentSpawnSpec
        {
            AgentId = "agent-2",
            Domain = "Economy",
            Goal = "Design economy",
            Dependencies = new[] { "agent-1" }
        };

        var container1 = factory.CreateContainer(spec1);
        var container2 = factory.CreateContainer(spec2);

        // Initialize agents to Ready state
        await container1.InitializeAsync();
        await container2.InitializeAsync();

        resolver.RegisterAgent(container1);
        resolver.RegisterAgent(container2);

        // Act
        var readyAgents = resolver.GetReadyAgents();

        // Assert
        readyAgents.Should().Contain(c => c.AgentId == "agent-1");
        readyAgents.Should().NotContain(c => c.AgentId == "agent-2"); // agent-2 depends on agent-1
    }

    [Fact]
    public async Task RecordOutput_UnblocksDependentAgents()
    {
        // Arrange
        var resolver = new DependencyResolver(_logger);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var spec1 = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system",
            Dependencies = Array.Empty<string>()
        };

        var spec2 = new AgentSpawnSpec
        {
            AgentId = "agent-2",
            Domain = "Economy",
            Goal = "Design economy",
            Dependencies = new[] { "agent-1" }
        };

        var container1 = factory.CreateContainer(spec1);
        var container2 = factory.CreateContainer(spec2);

        // Initialize agents to Ready state
        await container1.InitializeAsync();
        await container2.InitializeAsync();

        resolver.RegisterAgent(container1);
        resolver.RegisterAgent(container2);

        // Act
        resolver.RecordOutput("agent-1", new { Result = "output" });

        // Assert
        resolver.AreDependenciesResolved("agent-2").Should().BeTrue();
        var readyAgents = resolver.GetReadyAgents();
        readyAgents.Should().Contain(c => c.AgentId == "agent-2");
    }

    [Fact]
    public void GetExecutionOrder_ReturnsTopologicalOrder()
    {
        // Arrange
        var resolver = new DependencyResolver(_logger);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var spec1 = new AgentSpawnSpec
        {
            AgentId = "agent-1",
            Domain = "Combat",
            Goal = "Design system",
            Dependencies = Array.Empty<string>()
        };

        var spec2 = new AgentSpawnSpec
        {
            AgentId = "agent-2",
            Domain = "Economy",
            Goal = "Design economy",
            Dependencies = new[] { "agent-1" }
        };

        var container1 = factory.CreateContainer(spec1);
        var container2 = factory.CreateContainer(spec2);

        resolver.RegisterAgent(container1);
        resolver.RegisterAgent(container2);

        // Act
        var order = resolver.GetExecutionOrder();

        // Assert
        var orderList = order.ToList();
        var index1 = orderList.IndexOf("agent-1");
        var index2 = orderList.IndexOf("agent-2");
        index1.Should().BeLessThan(index2); // agent-1 should come before agent-2
    }

    [Fact]
    public async Task ChainOfCommandSupervisor_IsEnforcedAsDependency()
    {
        // Arrange
        var resolver = new DependencyResolver(_logger);
        var factory = new AgentFactory(
            _serviceProvider.GetRequiredService<ILogger<AgentFactory>>(),
            _serviceProvider);

        var commanderSpec = new AgentSpawnSpec
        {
            AgentId = "commander-1",
            Domain = "Strategy",
            Goal = "Set mission goals"
        };

        var squadSpec = new AgentSpawnSpec
        {
            AgentId = "squad-1",
            Domain = "Execution",
            Goal = "Execute mission goal",
            ReportsToAgentId = "commander-1"
        };

        var commanderContainer = factory.CreateContainer(commanderSpec);
        var squadContainer = factory.CreateContainer(squadSpec);
        await commanderContainer.InitializeAsync();
        await squadContainer.InitializeAsync();
        resolver.RegisterAgent(commanderContainer);
        resolver.RegisterAgent(squadContainer);

        // Act + Assert (before commander output)
        resolver.GetReadyAgents().Should().Contain(c => c.AgentId == "commander-1");
        resolver.GetReadyAgents().Should().NotContain(c => c.AgentId == "squad-1");
        resolver.AreDependenciesResolved("squad-1").Should().BeFalse();
        resolver.GetDependenciesForAgent("squad-1").Should().Contain("commander-1");

        resolver.RecordOutput("commander-1", new { acknowledged = true });

        // Assert (after commander output)
        resolver.AreDependenciesResolved("squad-1").Should().BeTrue();
        resolver.GetReadyAgents().Should().Contain(c => c.AgentId == "squad-1");
    }
}

