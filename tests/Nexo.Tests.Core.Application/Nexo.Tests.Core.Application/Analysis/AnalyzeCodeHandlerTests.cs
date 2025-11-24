using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;

namespace Nexo.Tests.Core.Application.Analysis;

/// <summary>
/// Unit tests for AnalyzeCodeHandler.
/// Tests the handler with mocked dependencies following Clean Architecture principles.
/// </summary>
public class AnalyzeCodeHandlerTests
{
    private readonly Mock<IAnalysisService> _mockAnalysisService;
    private readonly Mock<ILogger<AnalyzeCodeHandler>> _mockLogger;
    private readonly AnalyzeCodeHandler _handler;

    public AnalyzeCodeHandlerTests()
    {
        _mockAnalysisService = new Mock<IAnalysisService>();
        _mockLogger = new Mock<ILogger<AnalyzeCodeHandler>>();
        _handler = new AnalyzeCodeHandler(_mockAnalysisService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidPath_ReturnsAnalysisResult()
    {
        // Arrange
        var path = new DirectoryInfo("/test/path");
        var command = new AnalyzeCodeCommand(path);
        var expectedResult = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        _mockAnalysisService
            .Setup(x => x.AnalyzeAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasViolations);
        Assert.Equal(0, result.TotalViolations);
        _mockAnalysisService.Verify(x => x.AnalyzeAsync(path, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithViolations_ReturnsResultWithViolations()
    {
        // Arrange
        var path = new DirectoryInfo("/test/path");
        var command = new AnalyzeCodeCommand(path);
        var violations = new List<Violation>
        {
            new Violation
            {
                Rule = "SecurityScan",
                Message = "Security issue found",
                FilePath = "/test/file.cs",
                LineNumber = 10,
                Severity = Nexo.Core.Domain.Values.RiskLevel.High
            }
        };

        var expectedResult = new AnalysisResult
        {
            HasViolations = true,
            Violations = violations,
            TotalViolations = 1
        };

        _mockAnalysisService
            .Setup(x => x.AnalyzeAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasViolations);
        Assert.Equal(1, result.TotalViolations);
        Assert.Single(result.Violations);
        Assert.Equal("SecurityScan", result.Violations[0].Rule);
    }

    [Fact]
    public async Task Handle_LogsStartAndCompletion()
    {
        // Arrange
        var path = new DirectoryInfo("/test/path");
        var command = new AnalyzeCodeCommand(path);
        var expectedResult = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        _mockAnalysisService
            .Setup(x => x.AnalyzeAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting analysis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Analysis completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        // Arrange
        var path = new DirectoryInfo("/test/path");
        var command = new AnalyzeCodeCommand(path);
        var cancellationToken = new CancellationToken(true);
        var expectedResult = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        _mockAnalysisService
            .Setup(x => x.AnalyzeAsync(path, cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockAnalysisService.Verify(x => x.AnalyzeAsync(path, cancellationToken), Times.Once);
    }
}

