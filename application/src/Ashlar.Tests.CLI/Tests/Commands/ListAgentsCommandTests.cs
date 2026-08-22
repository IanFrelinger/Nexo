using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.CLI;
using Ashlar.CLI.Commands;
using Ashlar.CLI.Formatting;
using Ashlar.Core.Application.Agent.Models;
using Ashlar.Core.Application.Agent.UseCases.ListAgents;
using Ashlar.Core.Application.Testing.Abstractions;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for list agents command.</summary>
public class ListAgentsCommandTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test successful listing.</summary>
            await TestSuccessfulListing();
            /// <summary>Test empty agent list.</summary>
            await TestEmptyAgentList();
            /// <summary>Test json output.</summary>
            await TestJsonOutput();
            /// <summary>Test verbose output.</summary>
            await TestVerboseOutput();
            /// <summary>Test general exception.</summary>
            await TestGeneralException();

            return new TestResult
            {
                Name = nameof(ListAgentsCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All ListAgentsCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ListAgentsCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(ListAgentsCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulListing()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ListAgentsCommand>>();

        var agents = new List<AgentMetadata>
        {
            new AgentMetadata { Name = "Agent1", Description = "Test agent 1" },
            new AgentMetadata { Name = "Agent2", Description = "Test agent 2" }
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<ListAgentsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agents);

        var command = new ListAgentsCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(false, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderAgentList(agents, false), Times.Once);
    }

    private async Task TestEmptyAgentList()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ListAgentsCommand>>();

        var agents = new List<AgentMetadata>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<ListAgentsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agents);

        var command = new ListAgentsCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(false, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderAgentList(agents, false), Times.Once);
    }

    private async Task TestJsonOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ListAgentsCommand>>();

        var agents = new List<AgentMetadata>
        {
            new AgentMetadata { Name = "Agent1", Description = "Test agent 1" }
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<ListAgentsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agents);

        var command = new ListAgentsCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(true, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderAgentList(agents, true), Times.Once);
    }

    private async Task TestVerboseOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ListAgentsCommand>>();

        var agents = new List<AgentMetadata>
        {
            new AgentMetadata { Name = "Agent1", Description = "Test agent 1" }
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<ListAgentsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agents);

        var command = new ListAgentsCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(false, true);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderProgressStart(It.IsAny<string>()), Times.Once);
        mockRenderer.Verify(r => r.RenderProgressComplete(It.IsAny<string>()), Times.Once);
    }

    private async Task TestGeneralException()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<ListAgentsCommand>>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<ListAgentsQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var command = new ListAgentsCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync(false, false);

        AssertEqual((int)ExitCode.UnexpectedError, exitCode);
        mockRenderer.Verify(r => r.RenderError(It.IsAny<string>()), Times.Once);
    }
}

