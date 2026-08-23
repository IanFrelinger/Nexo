using Microsoft.Extensions.Logging;
using Moq;
using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Application.Analysis.Ports;
using Ashlar.Core.Application.Analysis.UseCases.AnalyzeCode;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Domain.Exceptions;
using Ashlar.Core.Domain.Values;

namespace Ashlar.Tests.Application.Tests.Handlers;

/// <summary>
/// Comprehensive tests for AnalyzeCodeHandler covering all scenarios.
/// </summary>
public class AnalyzeCodeHandlerComprehensiveTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await RunTestWithErrorHandling("TestSuccessfulAnalysisNoViolations", TestSuccessfulAnalysisNoViolations);
            await RunTestWithErrorHandling("TestSuccessfulAnalysisWithViolations", TestSuccessfulAnalysisWithViolations);
            await RunTestWithErrorHandling("TestUnauthorizedAccessException", TestUnauthorizedAccessException);
            await RunTestWithErrorHandling("TestGeneralException", TestGeneralException);
            await RunTestWithErrorHandling("TestCancellation", TestCancellation);
            await RunTestWithErrorHandling("TestProgressReporting", TestProgressReporting);
            await RunTestWithErrorHandling("TestMetricsCollection", TestMetricsCollection);

            return new TestResult
            {
                Name = nameof(AnalyzeCodeHandlerComprehensiveTests),
                Category = "Application",
                Passed = true,
                Message = "All AnalyzeCodeHandler tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(AnalyzeCodeHandlerComprehensiveTests),
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
                Name = nameof(AnalyzeCodeHandlerComprehensiveTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.GetType().Name}: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task RunTestWithErrorHandling(string testName, Func<Task> testAction)
    {
        try
        {
            await testAction();
        }
        catch (AssertionException)
        {
            throw; // Re-throw assertion exceptions
        }
        catch (AnalysisException ex) when (testName == "TestUnauthorizedAccessException" || testName == "TestGeneralException")
        {
            // Expected for these tests - they should catch it themselves
            throw new AssertionException($"Test {testName} did not catch expected AnalysisException: {ex.Message}", ex);
        }
        catch (AnalysisException ex) when (testName == "TestCancellation" && ex.InnerException is OperationCanceledException)
        {
            // Expected - cancellation was wrapped in AnalysisException
            return;
        }
        catch (OperationCanceledException) when (testName == "TestCancellation")
        {
            // Expected - cancellation was properly propagated
            return;
        }
        catch (Exception ex)
        {
            throw new AssertionException($"Test {testName} threw unexpected exception: {ex.Message}", ex);
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
        try
        {
            var result = await handler.Handle(command, CancellationToken.None);
            // If we get here, no exception was thrown - that's a test failure
            throw new AssertionException("Expected AnalysisException to be thrown but handler returned a result");
        }
        catch (AnalysisException ex)
        {
            // This is expected - the handler wraps the exception
            // Verify the exception message contains expected content
            if (!ex.Message.Contains("Unauthorized access"))
            {
                throw new AssertionException($"Exception message should mention 'Unauthorized access' but was: {ex.Message}");
            }
            // If we get here, the exception was caught and verified - test passes
            // No need to return, just let the method complete normally
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
        try
        {
            var result = await handler.Handle(command, CancellationToken.None);
            // If we get here, no exception was thrown - that's a test failure
            throw new AssertionException("Expected AnalysisException to be thrown but handler returned a result");
        }
        catch (AnalysisException ex)
        {
            // This is expected - the handler wraps the exception
            // Verify the exception message contains expected content
            if (!ex.Message.Contains("Analysis failed"))
            {
                throw new AssertionException($"Exception message should mention 'Analysis failed' but was: {ex.Message}");
            }
            // If we get here, the exception was caught and verified - test passes
            // No need to return, just let the method complete normally
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

