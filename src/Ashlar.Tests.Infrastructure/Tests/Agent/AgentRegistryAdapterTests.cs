using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Agent.Models;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Infrastructure.Agent.Adapters;

namespace Ashlar.Tests.Infrastructure.Tests.Agent;

/// <summary>Tests for agent registry adapter.</summary>
public class AgentRegistryAdapterTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test get agents async with no agents.</summary>
            await TestGetAgentsAsyncWithNoAgents();
            /// <summary>Test get agents async with single agent.</summary>
            await TestGetAgentsAsyncWithSingleAgent();
            /// <summary>Test get agents async with multiple agents.</summary>
            await TestGetAgentsAsyncWithMultipleAgents();
            /// <summary>Test get agent async found.</summary>
            await TestGetAgentAsyncFound();
            /// <summary>Test get agent async not found.</summary>
            await TestGetAgentAsyncNotFound();
            /// <summary>Test discover agents async.</summary>
            await TestDiscoverAgentsAsync();

            return new TestResult
            {
                Name = nameof(AgentRegistryAdapterTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All AgentRegistryAdapter tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(AgentRegistryAdapterTests),
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
                Name = nameof(AgentRegistryAdapterTests),
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

        /// <summary>Assert not null.</summary>
        AssertNotNull(agents);
        /// <summary>Assert equal.</summary>
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

        /// <summary>Assert not null.</summary>
        AssertNotNull(agents);
        /// <summary>Assert equal.</summary>
        AssertEqual(1, agents.Count);
        /// <summary>Assert equal.</summary>
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

        /// <summary>Assert not null.</summary>
        AssertNotNull(agents);
        /// <summary>Assert equal.</summary>
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

        /// <summary>Assert not null.</summary>
        AssertNotNull(agent);
        /// <summary>Assert equal.</summary>
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

        /// <summary>Assert null.</summary>
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

        /// <summary>Assert not null.</summary>
        AssertNotNull(agents);
        /// <summary>Assert equal.</summary>
        AssertEqual(1, agents.Count);
        /// <summary>Assert equal.</summary>
        AssertEqual("DiscoveredAgent", agents[0].Name);
    }
}

