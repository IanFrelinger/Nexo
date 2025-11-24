using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Core.Application.Validation.UseCases.RunValidation;

namespace Nexo.Tests.Core.Application.Validation;

/// <summary>
/// Unit tests for RunValidationHandler.
/// Tests the handler with mocked dependencies following Clean Architecture principles.
/// </summary>
public class RunValidationHandlerTests
{
    private readonly Mock<IValidationService> _mockValidationService;
    private readonly Mock<ILogger<RunValidationHandler>> _mockLogger;
    private readonly RunValidationHandler _handler;

    public RunValidationHandlerTests()
    {
        _mockValidationService = new Mock<IValidationService>();
        _mockLogger = new Mock<ILogger<RunValidationHandler>>();
        _handler = new RunValidationHandler(_mockValidationService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithNoFilter_ReturnsValidationResult()
    {
        // Arrange
        var command = new RunValidationCommand(null);
        var expectedResult = new ValidationResult
        {
            Passed = true,
            Message = "Validation passed",
            TestsRun = 5,
            TestsPassed = 5,
            TestsFailed = 0
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Passed);
        Assert.Equal(5, result.TestsRun);
        Assert.Equal(5, result.TestsPassed);
        Assert.Equal(0, result.TestsFailed);
        _mockValidationService.Verify(x => x.ValidateAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFilter_ReturnsValidationResult()
    {
        // Arrange
        var filter = "Category=Integration";
        var command = new RunValidationCommand(filter);
        var expectedResult = new ValidationResult
        {
            Passed = true,
            Message = "Validation passed",
            TestsRun = 3,
            TestsPassed = 3,
            TestsFailed = 0
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Passed);
        _mockValidationService.Verify(x => x.ValidateAsync(filter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFailedTests_ReturnsFailedResult()
    {
        // Arrange
        var command = new RunValidationCommand(null);
        var testResults = new List<TestResult>
        {
            new TestResult { Name = "Test1", Passed = true },
            new TestResult { Name = "Test2", Passed = false, Message = "Assertion failed" }
        };

        var expectedResult = new ValidationResult
        {
            Passed = false,
            Message = "Validation failed (1/2 tests failed)",
            TestsRun = 2,
            TestsPassed = 1,
            TestsFailed = 1,
            TestResults = testResults
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Passed);
        Assert.Equal(1, result.TestsFailed);
        Assert.NotNull(result.TestResults);
        Assert.Equal(2, result.TestResults.Count);
    }

    [Fact]
    public async Task Handle_LogsStartAndCompletion()
    {
        // Arrange
        var command = new RunValidationCommand(null);
        var expectedResult = new ValidationResult
        {
            Passed = true,
            Message = "Validation passed",
            TestsRun = 0,
            TestsPassed = 0,
            TestsFailed = 0
        };

        _mockValidationService
            .Setup(x => x.ValidateAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting validation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

