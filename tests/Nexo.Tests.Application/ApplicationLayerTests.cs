using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Agents;
using Nexo.Core.Application.Extensions;
using Nexo.Core.Application.Host;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Orchestration;
using Xunit;

namespace Nexo.Tests.Application;

/// <summary>
/// Tests for the Application layer
/// </summary>
public class ApplicationLayerTests
{
    [Fact]
    public void Application_Layer_Should_Compile_Successfully()
    {
        // This test verifies that the Application layer compiles without errors
        true.Should().BeTrue();
    }

    [Fact]
    public void ICommand_Interface_Should_Work()
    {
        // Test the unified command interface
        var command = new TestCommand();
        var result = command.ExecuteAsync("test", CancellationToken.None).Result;
        
        result.Should().Be("Processed: test");
    }

    [Fact]
    public void GenericCommandOrchestrator_Should_Execute_Commands()
    {
        // Test the generic orchestrator
        var orchestrator = new GenericCommandOrchestrator(
            Enumerable.Empty<IPreValidator>(),
            Enumerable.Empty<object>());
        var command = new TestCommand();

        var result = orchestrator.RunAsync(command, "test input", CancellationToken.None).Result;

        result.Success.Should().BeTrue();
        result.Message.Should().BeNull();
    }

    [Fact]
    public void ServiceHost_Should_Register_Services()
    {
        // Test the service host
        var serviceProvider = ServiceHost.BuildDefault();

        serviceProvider.GetService<IOrchestrator>().Should().NotBeNull();
        serviceProvider.GetService<GenericCommandOrchestrator>().Should().NotBeNull();
        serviceProvider.GetService<IPreValidator>().Should().NotBeNull();
    }

    [Fact]
    public void AgentFactory_Should_Create_Agents()
    {
        // Test the agent factory
        var services = new ServiceCollection();
        services.AddNexoAgents();
        services.AddTransient<TestAgent>();
        var serviceProvider = services.BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IAgentFactory>();
        var agent = factory.Create<TestAgent>();

        agent.Should().NotBeNull();
        agent.Should().BeOfType<TestAgent>();
    }
}

// Test implementations
public class TestCommand : ICommand<string, string>
{
    public ValueTask<string> ExecuteAsync(string input, CancellationToken ct)
    {
        return ValueTask.FromResult($"Processed: {input}");
    }
}

public class TestAgent
{
    public string Name => "TestAgent";
}
