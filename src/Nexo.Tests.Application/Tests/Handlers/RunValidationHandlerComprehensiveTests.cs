using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using ValidationTestResult = Nexo.Core.Application.Validation.Models.TestResult;
using Nexo.Core.Application.Validation.Models;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Core.Application.Validation.UseCases.RunValidation;
using Nexo.Core.Domain.Exceptions;

namespace Nexo.Tests.Application.Tests.Handlers;

/// <summary>
/// Comprehensive tests for RunValidationHandler covering all scenarios.
/// </summary>
public class RunValidationHandlerComprehensiveTests : UnitTestBase
{
    public override async Task<Nexo.Core.Application.Testing.Models.TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestSuccessfulValidationWithFilter();
            await TestSuccessfulValidationWithoutFilter();
            await TestFailedTests();
            await TestGeneralException();
            await TestCancellation();
            await TestProgressReporting();
            await TestMetricsCollection();

            return new Nexo.Core.Application.Testing.Models.TestResult
            {
                TestName = nameof(RunValidationHandlerComprehensiveTests),
                Category = "Application",
                Passed = true,
                Message = "All RunValidationHandler tests passed"
            };
        }
        catch (ValidationException)
        {
            // This is expected for exception tests - they should catch it internally
            // If we get here, it means the exception test didn't catch it properly
            return new Nexo.Core.Application.Testing.Models.TestResult
            {
                TestName = nameof(RunValidationHandlerComprehensiveTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = "Exception test did not properly catch ValidationException"
            };
        }
        catch (Exception ex)
        {
            return new Nexo.Core.Application.Testing.Models.TestResult
            {
                TestName = nameof(RunValidationHandlerComprehensiveTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulValidationWithFilter()
    {
        var mockService = new Mock<IValidationService>();
        var mockLogger = new Mock<ILogger<RunValidationHandler>>();
        var handler = new RunValidationHandler(mockService.Object, mockLogger.Object);

        var expectedResult = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 10,
            TestsPassed = 10,
            TestsFailed = 0,
            TestResults = new List<ValidationTestResult>()
        };

        mockService
            .Setup(s => s.ValidateAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunValidationCommand("TestFilter");
        var result = await handler.Handle(command, CancellationToken.None);

        AssertNotNull(result);
        AssertTrue(result.Passed);
        AssertEqual(10, result.TestsRun);
    }

    private async Task TestSuccessfulValidationWithoutFilter()
    {
        var mockService = new Mock<IValidationService>();
        var mockLogger = new Mock<ILogger<RunValidationHandler>>();
        var handler = new RunValidationHandler(mockService.Object, mockLogger.Object);

        var expectedResult = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 5,
            TestsPassed = 5,
            TestsFailed = 0
        };

        mockService
            .Setup(s => s.ValidateAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunValidationCommand(null);
        var result = await handler.Handle(command, CancellationToken.None);

        AssertNotNull(result);
        AssertTrue(result.Passed);
    }

    private async Task TestFailedTests()
    {
        var mockService = new Mock<IValidationService>();
        var mockLogger = new Mock<ILogger<RunValidationHandler>>();
        var handler = new RunValidationHandler(mockService.Object, mockLogger.Object);

        var expectedResult = new ValidationResult
        {
            Passed = false,
            Message = "Some tests failed",
            TestsRun = 10,
            TestsPassed = 8,
            TestsFailed = 2
        };

        mockService
            .Setup(s => s.ValidateAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunValidationCommand(null);
        var result = await handler.Handle(command, CancellationToken.None);

        AssertNotNull(result);
        AssertFalse(result.Passed);
        AssertEqual(2, result.TestsFailed);
    }

    private async Task TestGeneralException()
    {
        var mockService = new Mock<IValidationService>();
        var mockLogger = new Mock<ILogger<RunValidationHandler>>();
        var handler = new RunValidationHandler(mockService.Object, mockLogger.Object);

        mockService
            .Setup(s => s.ValidateAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var command = new RunValidationCommand(null);
        
        ValidationException? caughtException = null;
        try
        {
            await handler.Handle(command, CancellationToken.None);
        }
        catch (ValidationException ex)
        {
            caughtException = ex;
        }

        if (caughtException == null)
        {
            throw new Exception("Expected ValidationException to be thrown but none was thrown");
        }
        
        if (!caughtException.Message.Contains("Validation failed"))
        {
            throw new Exception($"Exception message should mention 'Validation failed' but was: {caughtException.Message}");
        }
    }

    private async Task TestCancellation()
    {
        var mockService = new Mock<IValidationService>();
        var mockLogger = new Mock<ILogger<RunValidationHandler>>();
        var handler = new RunValidationHandler(mockService.Object, mockLogger.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockService
            .Setup(s => s.ValidateAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var command = new RunValidationCommand(null);
        
        await AssertThrowsAsync<OperationCanceledException>(async () => 
            await handler.Handle(command, cts.Token), "Expected OperationCanceledException");
    }

    private async Task TestProgressReporting()
    {
        var mockService = new Mock<IValidationService>();
        var mockLogger = new Mock<ILogger<RunValidationHandler>>();
        var handler = new RunValidationHandler(mockService.Object, mockLogger.Object);

        var progress = new Progress<ProgressReport>();
        var expectedResult = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 0,
            TestsPassed = 0,
            TestsFailed = 0
        };

        mockService
            .Setup(s => s.ValidateAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunValidationCommand(null, progress);
        await handler.Handle(command, CancellationToken.None);

        mockService.Verify(s => s.ValidateAsync(
            It.IsAny<string?>(), 
            It.IsAny<IProgress<ProgressReport>>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestMetricsCollection()
    {
        var mockService = new Mock<IValidationService>();
        var mockLogger = new Mock<ILogger<RunValidationHandler>>();
        var mockMetrics = new Mock<IMetricsCollector>();
        var handler = new RunValidationHandler(mockService.Object, mockLogger.Object, mockMetrics.Object);

        var expectedResult = new ValidationResult
        {
            Passed = true,
            Message = "All tests passed",
            TestsRun = 10,
            TestsPassed = 8,
            TestsFailed = 2
        };

        mockService
            .Setup(s => s.ValidateAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunValidationCommand(null);
        await handler.Handle(command, CancellationToken.None);

        mockMetrics.Verify(m => m.RecordExecutionTime("Validation", It.IsAny<TimeSpan>()), Times.Once);
        mockMetrics.Verify(m => m.IncrementCounter(It.Is<string>(s => s == "Validation.Executed"), It.IsAny<int>()), Times.Once);
        mockMetrics.Verify(m => m.IncrementCounter(It.Is<string>(s => s == "Validation.TestsRun"), It.IsAny<int>()), Times.Once);
    }
}

