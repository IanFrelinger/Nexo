using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Agent.Ports;
using Nexo.Core.Application.Agent.UseCases.ListAgents;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.Application.Tests.Handlers;

public class ListAgentsHandlerTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSuccessfulListingWithAgents();
            await TestEmptyList();
            await TestCancellation();

            return new TestResult
            {
                TestName = nameof(ListAgentsHandlerTests),
                Category = "Application",
                Passed = true,
                Message = "All ListAgentsHandler tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(ListAgentsHandlerTests),
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
                TestName = nameof(ListAgentsHandlerTests),
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

        AssertNotNull(result);
        AssertEqual(2, result.Count);
        AssertEqual("TestAgent1", result[0].Name);
        AssertEqual("Test agent 1", result[0].Description);
        AssertEqual(2, result[0].Capabilities.Count);
        AssertEqual("TestAgent2", result[1].Name);
        AssertEqual("Test agent 2", result[1].Description);
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

        AssertNotNull(result);
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

