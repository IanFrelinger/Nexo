using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Agent.Models;
using Nexo.Core.Application.Agent.Ports;
using Nexo.Core.Application.Agent.UseCases.RunAgent;

namespace Nexo.Tests.Core.Application.Agent;

/// <summary>
/// Unit tests for RunAgentHandler.
/// Tests the handler with mocked dependencies following Clean Architecture principles.
/// </summary>
public class RunAgentHandlerTests
{
    private readonly Mock<IAgentExecutor> _mockAgentExecutor;
    private readonly Mock<ILogger<RunAgentHandler>> _mockLogger;
    private readonly RunAgentHandler _handler;

    public RunAgentHandlerTests()
    {
        _mockAgentExecutor = new Mock<IAgentExecutor>();
        _mockLogger = new Mock<ILogger<RunAgentHandler>>();
        _handler = new RunAgentHandler(_mockAgentExecutor.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidAgentName_ReturnsSuccessResult()
    {
        // Arrange
        var agentName = "director";
        var command = new RunAgentCommand(agentName, null);
        var expectedResult = new AgentExecutionResult
        {
            AgentName = agentName,
            Success = true,
            Message = $"Agent '{agentName}' executed successfully",
            Duration = TimeSpan.FromMilliseconds(100)
        };

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync(agentName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(agentName, result.AgentName);
        Assert.NotNull(result.Duration);
        _mockAgentExecutor.Verify(x => x.ExecuteAsync(agentName, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInputFile_ReturnsSuccessResult()
    {
        // Arrange
        var agentName = "director";
        var inputFile = new FileInfo("/test/input.dll");
        var command = new RunAgentCommand(agentName, inputFile);
        var expectedResult = new AgentExecutionResult
        {
            AgentName = agentName,
            Success = true,
            Message = $"Agent '{agentName}' executed successfully",
            Duration = TimeSpan.FromMilliseconds(150)
        };

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync(agentName, inputFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        _mockAgentExecutor.Verify(x => x.ExecuteAsync(agentName, inputFile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFailedExecution_ReturnsFailedResult()
    {
        // Arrange
        var agentName = "director";
        var command = new RunAgentCommand(agentName, null);
        var expectedResult = new AgentExecutionResult
        {
            AgentName = agentName,
            Success = false,
            Message = "Agent execution failed",
            Duration = TimeSpan.FromMilliseconds(50)
        };

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync(agentName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Agent execution failed", result.Message);
    }

    [Fact]
    public async Task Handle_WithTimeoutException_ReturnsFailedResult()
    {
        // Arrange
        var agentName = "director";
        var command = new RunAgentCommand(agentName, null);
        var timeoutException = new TimeoutException("Operation timed out");

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync(agentName, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(timeoutException);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("Timeout", result.Message);
        Assert.NotNull(result.Duration);
    }

    [Fact]
    public async Task Handle_LogsStartAndCompletion()
    {
        // Arrange
        var agentName = "director";
        var command = new RunAgentCommand(agentName, null);
        var expectedResult = new AgentExecutionResult
        {
            AgentName = agentName,
            Success = true,
            Message = $"Agent '{agentName}' executed successfully",
            Duration = TimeSpan.FromMilliseconds(100)
        };

        _mockAgentExecutor
            .Setup(x => x.ExecuteAsync(agentName, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting agent execution")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Agent execution completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

