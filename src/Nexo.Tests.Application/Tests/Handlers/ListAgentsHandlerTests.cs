using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Agent.Ports;
using Nexo.Core.Application.Agent.UseCases.ListAgents;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Handlers;

/// <summary>Tests for list agents handler.</summary>
public class ListAgentsHandlerTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test successful listing with agents.</summary>
            await TestSuccessfulListingWithAgents();
            /// <summary>Test empty list.</summary>
            await TestEmptyList();
            /// <summary>Test cancellation.</summary>
            await TestCancellation();

            return new TestResult
            {
                Name = nameof(ListAgentsHandlerTests),
                Category = "Application",
                Passed = true,
                Message = "All ListAgentsHandler tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ListAgentsHandlerTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(ListAgentsHandlerTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulListingWithAgents()
    {
        var mockAgentRegistry = new Mock<IAgentRegistry>();
        var mockLogger = new Mock<ILogger<ListAgentsHandler>>();
        var handler = new ListAgentsHandler(mockAgentRegistry.Object, mockLogger.Object);

        var expectedAgents = new List<AgentMetadata>
        {
            new AgentMetadata
            {
                Name = "TestAgent1",
                Description = "Test agent 1",
                Capabilities = new List<string> { "capability1", "capability2" },
                Parameters = new Dictionary<string, string> { { "param1", "value1" } }
            },
            new AgentMetadata
            {
                Name = "TestAgent2",
                Description = "Test agent 2",
                Capabilities = new List<string> { "capability3" },
                Parameters = new Dictionary<string, string>()
            }
        };

        mockAgentRegistry
            .Setup(r => r.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAgents);

        var query = new ListAgentsQuery();
        var result = await handler.Handle(query, CancellationToken.None);

        /// <summary>Assert not null.</summary>
        AssertNotNull(result);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, result.Count);
        /// <summary>Assert equal.</summary>
        AssertEqual("TestAgent1", result[0].Name);
        /// <summary>Assert equal.</summary>
        /// <param name="1"">1".</param>
        AssertEqual("Test agent 1", result[0].Description);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, result[0].Capabilities.Count);
        /// <summary>Assert equal.</summary>
        AssertEqual("TestAgent2", result[1].Name);
        /// <summary>Assert equal.</summary>
        /// <param name="2"">2".</param>
        AssertEqual("Test agent 2", result[1].Description);
        /// <summary>Assert equal.</summary>
        AssertEqual(1, result[1].Capabilities.Count);

        mockAgentRegistry.Verify(r => r.GetAgentsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestEmptyList()
    {
        var mockAgentRegistry = new Mock<IAgentRegistry>();
        var mockLogger = new Mock<ILogger<ListAgentsHandler>>();
        var handler = new ListAgentsHandler(mockAgentRegistry.Object, mockLogger.Object);

        var emptyAgents = new List<AgentMetadata>();

        mockAgentRegistry
            .Setup(r => r.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyAgents);

        var query = new ListAgentsQuery();
        var result = await handler.Handle(query, CancellationToken.None);

        /// <summary>Assert not null.</summary>
        AssertNotNull(result);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, result.Count);

        mockAgentRegistry.Verify(r => r.GetAgentsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestCancellation()
    {
        var mockAgentRegistry = new Mock<IAgentRegistry>();
        var mockLogger = new Mock<ILogger<ListAgentsHandler>>();
        var handler = new ListAgentsHandler(mockAgentRegistry.Object, mockLogger.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockAgentRegistry
            .Setup(r => r.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var query = new ListAgentsQuery();

        await AssertThrowsAsync<OperationCanceledException>(() => handler.Handle(query, cts.Token));
    }
}

