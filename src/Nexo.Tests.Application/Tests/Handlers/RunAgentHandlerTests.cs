using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Agent.Ports;
using Nexo.Core.Application.Agent.UseCases.RunAgent;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Tests.Application.Tests.Handlers;

/// <summary>
/// Comprehensive tests for RunAgentHandler covering all scenarios.
/// </summary>
public class RunAgentHandlerTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSuccessfulExecution();
            await TestFailedExecution();
            await TestWithInputFile();
            await TestWithoutInputFile();
            await TestTimeoutException();
            await TestGeneralException();
            await TestMetricsCollection();

            return new TestResult
            {
                TestName = nameof(RunAgentHandlerTests),
                Category = "Application",
                Passed = true,
                Message = "All RunAgentHandler tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(RunAgentHandlerTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulExecution()
    {
        var mockExecutor = new Mock<IAgentExecutor>();
        var mockLogger = new Mock<ILogger<RunAgentHandler>>();
        var handler = new RunAgentHandler(mockExecutor.Object, mockLogger.Object);

        var expectedResult = new AgentExecutionResult
        {
            AgentName = "test-agent",
            Success = true,
            Message = "Execution successful",
            Output = "Test output"
        };

        mockExecutor
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<FileInfo?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunAgentCommand("test-agent", null);
        var result = await handler.Handle(command, CancellationToken.None);

        AssertNotNull(result);
        AssertTrue(result.Success);
        AssertEqual("test-agent", result.AgentName);
    }

    private async Task TestFailedExecution()
    {
        var mockExecutor = new Mock<IAgentExecutor>();
        var mockLogger = new Mock<ILogger<RunAgentHandler>>();
        var handler = new RunAgentHandler(mockExecutor.Object, mockLogger.Object);

        var expectedResult = new AgentExecutionResult
        {
            AgentName = "test-agent",
            Success = false,
            Message = "Execution failed",
            ErrorMessage = "Test error"
        };

        mockExecutor
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<FileInfo?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunAgentCommand("test-agent", null);
        var result = await handler.Handle(command, CancellationToken.None);

        AssertNotNull(result);
        AssertFalse(result.Success);
    }

    private async Task TestWithInputFile()
    {
        var mockExecutor = new Mock<IAgentExecutor>();
        var mockLogger = new Mock<ILogger<RunAgentHandler>>();
        var handler = new RunAgentHandler(mockExecutor.Object, mockLogger.Object);

        var inputFile = new FileInfo(Path.GetTempFileName());
        var expectedResult = new AgentExecutionResult
        {
            AgentName = "test-agent",
            Success = true,
            Message = "Execution successful"
        };

        mockExecutor
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<FileInfo?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunAgentCommand("test-agent", inputFile);
        var result = await handler.Handle(command, CancellationToken.None);

        AssertNotNull(result);
        mockExecutor.Verify(e => e.ExecuteAsync("test-agent", inputFile, It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestWithoutInputFile()
    {
        var mockExecutor = new Mock<IAgentExecutor>();
        var mockLogger = new Mock<ILogger<RunAgentHandler>>();
        var handler = new RunAgentHandler(mockExecutor.Object, mockLogger.Object);

        var expectedResult = new AgentExecutionResult
        {
            AgentName = "test-agent",
            Success = true,
            Message = "Execution successful"
        };

        mockExecutor
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<FileInfo?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunAgentCommand("test-agent", null);
        await handler.Handle(command, CancellationToken.None);

        mockExecutor.Verify(e => e.ExecuteAsync("test-agent", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestTimeoutException()
    {
        var mockExecutor = new Mock<IAgentExecutor>();
        var mockLogger = new Mock<ILogger<RunAgentHandler>>();
        var handler = new RunAgentHandler(mockExecutor.Object, mockLogger.Object);

        mockExecutor
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<FileInfo?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Timeout"));

        var command = new RunAgentCommand("test-agent", null);
        
        await AssertThrowsAsync<AgentExecutionException>(async () => 
            await handler.Handle(command, CancellationToken.None), "Expected AgentExecutionException");
    }

    private async Task TestGeneralException()
    {
        var mockExecutor = new Mock<IAgentExecutor>();
        var mockLogger = new Mock<ILogger<RunAgentHandler>>();
        var handler = new RunAgentHandler(mockExecutor.Object, mockLogger.Object);

        mockExecutor
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<FileInfo?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var command = new RunAgentCommand("test-agent", null);
        
        await AssertThrowsAsync<AgentExecutionException>(async () => 
            await handler.Handle(command, CancellationToken.None), "Expected AgentExecutionException");
    }

    private async Task TestMetricsCollection()
    {
        var mockExecutor = new Mock<IAgentExecutor>();
        var mockLogger = new Mock<ILogger<RunAgentHandler>>();
        var mockMetrics = new Mock<IMetricsCollector>();
        var handler = new RunAgentHandler(mockExecutor.Object, mockLogger.Object, mockMetrics.Object);

        var expectedResult = new AgentExecutionResult
        {
            AgentName = "test-agent",
            Success = true,
            Message = "Execution successful"
        };

        mockExecutor
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<FileInfo?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunAgentCommand("test-agent", null);
        await handler.Handle(command, CancellationToken.None);

        mockMetrics.Verify(m => m.RecordExecutionTime(It.Is<string>(s => s == "Agent.test-agent"), It.IsAny<TimeSpan>()), Times.Once);
        mockMetrics.Verify(m => m.IncrementCounter(It.Is<string>(s => s == "Agent.Executed"), It.IsAny<int>()), Times.Once);
        mockMetrics.Verify(m => m.IncrementCounter(It.Is<string>(s => s == "Agent.Success"), It.IsAny<int>()), Times.Once);
    }
}

