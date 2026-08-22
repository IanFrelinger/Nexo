using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.CLI;
using Ashlar.CLI.Commands;
using Ashlar.CLI.Formatting;
using Ashlar.Core.Application.Agent.Models;
using Ashlar.Core.Application.Agent.UseCases.RunAgent;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Domain.Exceptions;
using Ashlar.Tests.Application.Helpers;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>Tests for agent command.</summary>
public class AgentCommandTests : UnitTestBase
{
    private DirectoryInfo? _tempDir;

    public override Task SetupAsync(CancellationToken cancellationToken = default)
    {
        _tempDir = TestHelpers.CreateTempDirectory();
        return Task.CompletedTask;
    }

    public override Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        if (_tempDir != null)
        {
            TestHelpers.CleanupTempDirectory(_tempDir);
        }
        return Task.CompletedTask;
    }

    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test successful execution.</summary>
            await TestSuccessfulExecution();
            /// <summary>Test failed execution.</summary>
            await TestFailedExecution();
            /// <summary>Test json output.</summary>
            await TestJsonOutput();
            /// <summary>Test verbose output.</summary>
            await TestVerboseOutput();
            /// <summary>Test agent execution exception.</summary>
            await TestAgentExecutionException();
            /// <summary>Test timeout exception.</summary>
            await TestTimeoutException();
            /// <summary>Test general exception.</summary>
            await TestGeneralException();

            return new TestResult
            {
                Name = nameof(AgentCommandTests),
                Category = "CLI",
                Passed = true,
                Message = "All AgentCommand tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(AgentCommandTests),
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
                Name = nameof(AgentCommandTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulExecution()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AgentCommand>>();

        var result = new AgentExecutionResult
        {
            AgentName = "TestAgent",
            Success = true,
            Message = "Agent executed successfully",
            Duration = TimeSpan.FromSeconds(1)
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunAgentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new AgentCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync("TestAgent", null, false, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderAgentResult(result, false), Times.Once);
    }

    private async Task TestFailedExecution()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AgentCommand>>();

        var result = new AgentExecutionResult
        {
            AgentName = "TestAgent",
            Success = false,
            Message = "Agent execution failed",
            Duration = TimeSpan.FromSeconds(1)
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunAgentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new AgentCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync("TestAgent", null, false, false);

        AssertEqual((int)ExitCode.ValidationFailed, exitCode);
        mockRenderer.Verify(r => r.RenderAgentResult(result, false), Times.Once);
    }

    private async Task TestJsonOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AgentCommand>>();

        var result = new AgentExecutionResult
        {
            AgentName = "TestAgent",
            Success = true,
            Message = "Agent executed successfully",
            Duration = TimeSpan.FromSeconds(1)
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunAgentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new AgentCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync("TestAgent", null, true, false);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderAgentResult(result, true), Times.Once);
    }

    private async Task TestVerboseOutput()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AgentCommand>>();

        var result = new AgentExecutionResult
        {
            AgentName = "TestAgent",
            Success = true,
            Message = "Agent executed successfully",
            Duration = TimeSpan.FromSeconds(1)
        };

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunAgentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var command = new AgentCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync("TestAgent", null, false, true);

        AssertEqual((int)ExitCode.Ok, exitCode);
        mockRenderer.Verify(r => r.RenderProgressStart(It.IsAny<string>()), Times.Once);
        mockRenderer.Verify(r => r.RenderProgressComplete(It.IsAny<string>()), Times.Once);
    }

    private async Task TestAgentExecutionException()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AgentCommand>>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunAgentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AgentExecutionException("TestAgent", "Agent execution failed", ErrorCodes.AgentExecutionFailed));

        var command = new AgentCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync("TestAgent", null, false, false);

        AssertEqual((int)ExitCode.ValidationFailed, exitCode);
        mockRenderer.Verify(r => r.RenderErrorWithCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    private async Task TestTimeoutException()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AgentCommand>>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunAgentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Agent execution timed out"));

        var command = new AgentCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync("TestAgent", null, false, false);

        AssertEqual((int)ExitCode.ValidationFailed, exitCode);
        mockRenderer.Verify(r => r.RenderError(It.Is<string>(s => s.Contains("Timeout"))), Times.Once);
    }

    private async Task TestGeneralException()
    {
        var mockMediator = new Mock<IMediator>();
        var mockRenderer = new Mock<IConsoleRenderer>();
        var mockLogger = new Mock<ILogger<AgentCommand>>();

        mockMediator
            .Setup(m => m.Send(It.IsAny<RunAgentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var command = new AgentCommand(mockMediator.Object, mockRenderer.Object, mockLogger.Object);
        var exitCode = await command.ExecuteAsync("TestAgent", null, false, false);

        AssertEqual((int)ExitCode.UnexpectedError, exitCode);
        mockRenderer.Verify(r => r.RenderError(It.IsAny<string>()), Times.Once);
    }
}

