using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Abstractions;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Infrastructure.Agent.Adapters;

namespace Nexo.Tests.Infrastructure.Tests.Agent;

public class AgentRegistryAdapterTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestGetAgentsAsyncWithNoAgents();
            await TestGetAgentsAsyncWithSingleAgent();
            await TestGetAgentsAsyncWithMultipleAgents();
            await TestGetAgentAsyncFound();
            await TestGetAgentAsyncNotFound();
            await TestDiscoverAgentsAsync();

            return new TestResult
            {
                TestName = nameof(AgentRegistryAdapterTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All AgentRegistryAdapter tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(AgentRegistryAdapterTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(AgentRegistryAdapterTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestGetAgentsAsyncWithNoAgents()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<AgentRegistryAdapter>>();

        // Mock GetServices to return empty collection
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IEnumerable<IAgent>)))
            .Returns(Array.Empty<IAgent>());

        var adapter = new AgentRegistryAdapter(mockServiceProvider.Object, mockLogger.Object);
        var agents = await adapter.GetAgentsAsync();

        AssertNotNull(agents);
        AssertEqual(0, agents.Count);
    }

    private async Task TestGetAgentsAsyncWithSingleAgent()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<AgentRegistryAdapter>>();
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("TestAgent");

        var agentsEnumerable = new[] { mockAgent.Object };
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IEnumerable<IAgent>)))
            .Returns(agentsEnumerable);

        var adapter = new AgentRegistryAdapter(mockServiceProvider.Object, mockLogger.Object);
        var agents = await adapter.GetAgentsAsync();

        AssertNotNull(agents);
        AssertEqual(1, agents.Count);
        AssertEqual("TestAgent", agents[0].Name);
    }

    private async Task TestGetAgentsAsyncWithMultipleAgents()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<AgentRegistryAdapter>>();
        
        var mockAgent1 = new Mock<IAgent>();
        mockAgent1.Setup(a => a.Name).Returns("Agent1");
        var mockAgent2 = new Mock<IAgent>();
        mockAgent2.Setup(a => a.Name).Returns("Agent2");
        var mockAgent3 = new Mock<IAgent>();
        mockAgent3.Setup(a => a.Name).Returns("Agent3");

        var agentsEnumerable = new[] { mockAgent1.Object, mockAgent2.Object, mockAgent3.Object };
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IEnumerable<IAgent>)))
            .Returns(agentsEnumerable);

        var adapter = new AgentRegistryAdapter(mockServiceProvider.Object, mockLogger.Object);
        var agents = await adapter.GetAgentsAsync();

        AssertNotNull(agents);
        AssertEqual(3, agents.Count);
        AssertTrue(agents.Any(a => a.Name == "Agent1"));
        AssertTrue(agents.Any(a => a.Name == "Agent2"));
        AssertTrue(agents.Any(a => a.Name == "Agent3"));
    }

    private async Task TestGetAgentAsyncFound()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<AgentRegistryAdapter>>();
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("TestAgent");

        var agentsEnumerable = new[] { mockAgent.Object };
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IEnumerable<IAgent>)))
            .Returns(agentsEnumerable);

        var adapter = new AgentRegistryAdapter(mockServiceProvider.Object, mockLogger.Object);
        var agent = await adapter.GetAgentAsync("TestAgent");

        AssertNotNull(agent);
        AssertEqual("TestAgent", agent!.Name);
    }

    private async Task TestGetAgentAsyncNotFound()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<AgentRegistryAdapter>>();

        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IEnumerable<IAgent>)))
            .Returns(Array.Empty<IAgent>());

        var adapter = new AgentRegistryAdapter(mockServiceProvider.Object, mockLogger.Object);
        var agent = await adapter.GetAgentAsync("NonExistentAgent");

        AssertNull(agent);
    }

    private async Task TestDiscoverAgentsAsync()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<AgentRegistryAdapter>>();
        var mockAgent = new Mock<IAgent>();
        mockAgent.Setup(a => a.Name).Returns("DiscoveredAgent");

        var agentsEnumerable = new[] { mockAgent.Object };
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IEnumerable<IAgent>)))
            .Returns(agentsEnumerable);

        var adapter = new AgentRegistryAdapter(mockServiceProvider.Object, mockLogger.Object);
        var agents = await adapter.DiscoverAgentsAsync();

        AssertNotNull(agents);
        AssertEqual(1, agents.Count);
        AssertEqual("DiscoveredAgent", agents[0].Name);
    }
}

