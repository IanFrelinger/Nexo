using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.Analysis.UseCases.AnalyzeCode;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Exceptions;
using Nexo.Core.Domain.Values;

namespace Nexo.Tests.Application.Tests.Handlers;

/// <summary>
/// Comprehensive tests for AnalyzeCodeHandler covering all scenarios.
/// </summary>
public class AnalyzeCodeHandlerComprehensiveTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Test 1: Successful analysis with no violations
            await TestSuccessfulAnalysisNoViolations();
            
            // Test 2: Successful analysis with violations
            await TestSuccessfulAnalysisWithViolations();
            
            // Test 3: UnauthorizedAccessException handling
            await TestUnauthorizedAccessException();
            
            // Test 4: General exception handling
            await TestGeneralException();
            
            // Test 5: Cancellation token propagation
            await TestCancellation();
            
            // Test 6: Progress reporting
            await TestProgressReporting();
            
            // Test 7: Metrics collection
            await TestMetricsCollection();

            return new TestResult
            {
                TestName = nameof(AnalyzeCodeHandlerComprehensiveTests),
                Category = "Application",
                Passed = true,
                Message = "All AnalyzeCodeHandler tests passed"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(AnalyzeCodeHandlerComprehensiveTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulAnalysisNoViolations()
    {
        var mockService = new Mock<IAnalysisService>();
        var mockLogger = new Mock<ILogger<AnalyzeCodeHandler>>();
        var handler = new AnalyzeCodeHandler(mockService.Object, mockLogger.Object);

        var expectedResult = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        mockService
            .Setup(s => s.AnalyzeAsync(It.IsAny<DirectoryInfo>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new AnalyzeCodeCommand(new DirectoryInfo(Path.GetTempPath()));
        var result = await handler.Handle(command, CancellationToken.None);

        AssertNotNull(result);
        AssertFalse(result.HasViolations);
        AssertEqual(0, result.TotalViolations);
    }

    private async Task TestSuccessfulAnalysisWithViolations()
    {
        var mockService = new Mock<IAnalysisService>();
        var mockLogger = new Mock<ILogger<AnalyzeCodeHandler>>();
        var handler = new AnalyzeCodeHandler(mockService.Object, mockLogger.Object);

        var expectedResult = new AnalysisResult
        {
            HasViolations = true,
            Violations = new List<Violation>
            {
                new Violation { Rule = "TestRule", Message = "Test violation", FilePath = "test.cs", Severity = RiskLevel.High }
            },
            TotalViolations = 1
        };

        mockService
            .Setup(s => s.AnalyzeAsync(It.IsAny<DirectoryInfo>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new AnalyzeCodeCommand(new DirectoryInfo(Path.GetTempPath()));
        var result = await handler.Handle(command, CancellationToken.None);

        AssertNotNull(result);
        AssertTrue(result.HasViolations);
        AssertEqual(1, result.TotalViolations);
    }

    private async Task TestUnauthorizedAccessException()
    {
        var mockService = new Mock<IAnalysisService>();
        var mockLogger = new Mock<ILogger<AnalyzeCodeHandler>>();
        var handler = new AnalyzeCodeHandler(mockService.Object, mockLogger.Object);

        mockService
            .Setup(s => s.AnalyzeAsync(It.IsAny<DirectoryInfo>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        var command = new AnalyzeCodeCommand(new DirectoryInfo(Path.GetTempPath()));
        
        // The handler should catch the UnauthorizedAccessException and wrap it in an AnalysisException
        // We expect an AnalysisException to be thrown, so we catch it and verify it
        bool exceptionThrown = false;
        
        try
        {
            var result = await handler.Handle(command, CancellationToken.None);
            // If we get here, no exception was thrown - that's a test failure
            throw new AssertionException("Expected AnalysisException to be thrown but handler returned a result");
        }
        catch (AnalysisException ex)
        {
            // This is expected - the handler wraps the exception
            exceptionThrown = true;
            
            // Verify the exception message contains expected content
            if (!ex.Message.Contains("Unauthorized access"))
            {
                throw new AssertionException($"Exception message should mention 'Unauthorized access' but was: {ex.Message}");
            }
        }
        catch (AssertionException)
        {
            // Re-throw assertion exceptions
            throw;
        }
        catch (Exception ex)
        {
            // If we catch a different exception, that's also a failure
            throw new AssertionException($"Expected AnalysisException but got {ex.GetType().Name}: {ex.Message}");
        }

        // Verify the exception was thrown
        if (!exceptionThrown)
        {
            throw new AssertionException("Expected AnalysisException to be thrown but none was caught");
        }
    }

    private async Task TestGeneralException()
    {
        var mockService = new Mock<IAnalysisService>();
        var mockLogger = new Mock<ILogger<AnalyzeCodeHandler>>();
        var handler = new AnalyzeCodeHandler(mockService.Object, mockLogger.Object);

        mockService
            .Setup(s => s.AnalyzeAsync(It.IsAny<DirectoryInfo>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var command = new AnalyzeCodeCommand(new DirectoryInfo(Path.GetTempPath()));
        
        // The handler should catch the InvalidOperationException and wrap it in an AnalysisException
        // We expect an AnalysisException to be thrown, so we catch it and verify it
        bool exceptionThrown = false;
        string? exceptionMessage = null;
        
        try
        {
            var result = await handler.Handle(command, CancellationToken.None);
            // If we get here, no exception was thrown - that's a test failure
            throw new AssertionException("Expected AnalysisException to be thrown but handler returned a result");
        }
        catch (AnalysisException ex)
        {
            // This is expected - the handler wraps the exception
            exceptionThrown = true;
            exceptionMessage = ex.Message;
            
            // Verify the exception message contains expected content
            if (!ex.Message.Contains("Analysis failed"))
            {
                throw new AssertionException($"Exception message should mention 'Analysis failed' but was: {ex.Message}");
            }
        }
        catch (AssertionException)
        {
            // Re-throw assertion exceptions
            throw;
        }
        catch (Exception ex)
        {
            // If we catch a different exception, that's also a failure
            throw new AssertionException($"Expected AnalysisException but got {ex.GetType().Name}: {ex.Message}");
        }

        // Verify the exception was thrown
        if (!exceptionThrown)
        {
            throw new AssertionException("Expected AnalysisException to be thrown but none was caught");
        }
    }

    private async Task TestCancellation()
    {
        var mockService = new Mock<IAnalysisService>();
        var mockLogger = new Mock<ILogger<AnalyzeCodeHandler>>();
        var handler = new AnalyzeCodeHandler(mockService.Object, mockLogger.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockService
            .Setup(s => s.AnalyzeAsync(It.IsAny<DirectoryInfo>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var command = new AnalyzeCodeCommand(new DirectoryInfo(Path.GetTempPath()));
        
        await AssertThrowsAsync<OperationCanceledException>(async () => 
            await handler.Handle(command, cts.Token));
    }

    private async Task TestProgressReporting()
    {
        var mockService = new Mock<IAnalysisService>();
        var mockLogger = new Mock<ILogger<AnalyzeCodeHandler>>();
        var handler = new AnalyzeCodeHandler(mockService.Object, mockLogger.Object);

        var progress = new Progress<ProgressReport>();
        var reports = new List<ProgressReport>();
        progress.ProgressChanged += (_, report) => reports.Add(report);

        var expectedResult = new AnalysisResult
        {
            HasViolations = false,
            Violations = Array.Empty<Violation>(),
            TotalViolations = 0
        };

        mockService
            .Setup(s => s.AnalyzeAsync(It.IsAny<DirectoryInfo>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new AnalyzeCodeCommand(new DirectoryInfo(Path.GetTempPath()), progress);
        await handler.Handle(command, CancellationToken.None);

        // Progress should be passed to service
        mockService.Verify(s => s.AnalyzeAsync(
            It.IsAny<DirectoryInfo>(), 
            It.IsAny<IProgress<ProgressReport>>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestMetricsCollection()
    {
        var mockService = new Mock<IAnalysisService>();
        var mockLogger = new Mock<ILogger<AnalyzeCodeHandler>>();
        var mockMetrics = new Mock<IMetricsCollector>();
        var handler = new AnalyzeCodeHandler(mockService.Object, mockLogger.Object, mockMetrics.Object);

        var expectedResult = new AnalysisResult
        {
            HasViolations = true,
            Violations = new List<Violation> { new Violation { Rule = "Test", Message = "Test", FilePath = "test.cs", Severity = RiskLevel.High } },
            TotalViolations = 1
        };

        mockService
            .Setup(s => s.AnalyzeAsync(It.IsAny<DirectoryInfo>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new AnalyzeCodeCommand(new DirectoryInfo(Path.GetTempPath()));
        await handler.Handle(command, CancellationToken.None);

        mockMetrics.Verify(m => m.RecordExecutionTime("Analysis", It.IsAny<TimeSpan>()), Times.Once);
        mockMetrics.Verify(m => m.IncrementCounter(It.Is<string>(s => s == "Analysis.Executed"), It.IsAny<int>()), Times.Once);
        mockMetrics.Verify(m => m.IncrementCounter(It.Is<string>(s => s == "Analysis.Violations"), It.Is<int>(v => v == 1)), Times.Once);
    }

}

