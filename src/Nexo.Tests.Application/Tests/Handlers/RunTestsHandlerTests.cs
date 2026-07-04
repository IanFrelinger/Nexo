using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Application.Testing.Ports;
using Nexo.Core.Application.Testing.UseCases.RunTests;

namespace Nexo.Tests.Application.Tests.Handlers;

/// <summary>Tests for run tests handler.</summary>
public class RunTestsHandlerTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            /// <summary>Test successful execution without filter.</summary>
            await TestSuccessfulExecutionWithoutFilter();
            /// <summary>Test successful execution with filter.</summary>
            await TestSuccessfulExecutionWithFilter();
            /// <summary>Test cancellation.</summary>
            await TestCancellation();
            /// <summary>Test progress reporting.</summary>
            await TestProgressReporting();
            /// <summary>Test empty results.</summary>
            await TestEmptyResults();

            return new TestResult
            {
                Name = nameof(RunTestsHandlerTests),
                Category = "Application",
                Passed = true,
                Message = "All RunTestsHandler tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(RunTestsHandlerTests),
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
                Name = nameof(RunTestsHandlerTests),
                Category = "Application",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestSuccessfulExecutionWithoutFilter()
    {
        var mockTestRunner = new Mock<ITestRunner>();
        var mockLogger = new Mock<ILogger<RunTestsHandler>>();
        var handler = new RunTestsHandler(mockTestRunner.Object, mockLogger.Object);

        var expectedResult = new TestExecutionResult
        {
            TotalTests = 3,
            PassedTests = 2,
            FailedTests = 1,
            TotalDuration = TimeSpan.FromMilliseconds(100),
            Results = new List<TestResult>
            {
                new TestResult { Name = "Test1", Category = "Category1", Passed = true, Duration = TimeSpan.FromMilliseconds(50) },
                new TestResult { Name = "Test2", Category = "Category1", Passed = true, Duration = TimeSpan.FromMilliseconds(30) },
                new TestResult { Name = "Test3", Category = "Category2", Passed = false, Duration = TimeSpan.FromMilliseconds(20) }
            }
        };

        mockTestRunner
            .Setup(r => r.RunTestsAsync(null, It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunTestsCommand(null);
        var result = await handler.Handle(command, CancellationToken.None);

        /// <summary>Assert not null.</summary>
        AssertNotNull(result);
        /// <summary>Assert equal.</summary>
        AssertEqual(3, result.TotalTests);
        /// <summary>Assert equal.</summary>
        AssertEqual(2, result.PassedTests);
        /// <summary>Assert equal.</summary>
        AssertEqual(1, result.FailedTests);
        AssertEqual(TimeSpan.FromMilliseconds(100), result.TotalDuration);
        /// <summary>Assert equal.</summary>
        AssertEqual(3, result.Results.Count);

        mockTestRunner.Verify(r => r.RunTestsAsync(null, It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestSuccessfulExecutionWithFilter()
    {
        var mockTestRunner = new Mock<ITestRunner>();
        var mockLogger = new Mock<ILogger<RunTestsHandler>>();
        var handler = new RunTestsHandler(mockTestRunner.Object, mockLogger.Object);

        var expectedResult = new TestExecutionResult
        {
            TotalTests = 1,
            PassedTests = 1,
            FailedTests = 0,
            TotalDuration = TimeSpan.FromMilliseconds(50),
            Results = new List<TestResult>
            {
                new TestResult { Name = "Test1", Category = "Category1", Passed = true, Duration = TimeSpan.FromMilliseconds(50) }
            }
        };

        mockTestRunner
            .Setup(r => r.RunTestsAsync("Category1", It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunTestsCommand("Category1");
        var result = await handler.Handle(command, CancellationToken.None);

        /// <summary>Assert not null.</summary>
        AssertNotNull(result);
        /// <summary>Assert equal.</summary>
        AssertEqual(1, result.TotalTests);
        /// <summary>Assert equal.</summary>
        AssertEqual(1, result.PassedTests);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, result.FailedTests);

        mockTestRunner.Verify(r => r.RunTestsAsync("Category1", It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestCancellation()
    {
        var mockTestRunner = new Mock<ITestRunner>();
        var mockLogger = new Mock<ILogger<RunTestsHandler>>();
        var handler = new RunTestsHandler(mockTestRunner.Object, mockLogger.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        mockTestRunner
            .Setup(r => r.RunTestsAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var command = new RunTestsCommand(null);

        await AssertThrowsAsync<OperationCanceledException>(() => handler.Handle(command, cts.Token));
    }

    private async Task TestProgressReporting()
    {
        var mockTestRunner = new Mock<ITestRunner>();
        var mockLogger = new Mock<ILogger<RunTestsHandler>>();
        var handler = new RunTestsHandler(mockTestRunner.Object, mockLogger.Object);

        var expectedResult = new TestExecutionResult
        {
            TotalTests = 1,
            PassedTests = 1,
            FailedTests = 0,
            TotalDuration = TimeSpan.FromMilliseconds(50),
            Results = new List<TestResult>
            {
                new TestResult { Name = "Test1", Category = "Category1", Passed = true, Duration = TimeSpan.FromMilliseconds(50) }
            }
        };

        var progress = new Progress<ProgressReport>();
        ProgressReport? capturedProgress = null;
        progress.ProgressChanged += (sender, report) => capturedProgress = report;

        mockTestRunner
            .Setup(r => r.RunTestsAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunTestsCommand(null, progress);
        var result = await handler.Handle(command, CancellationToken.None);

        /// <summary>Assert not null.</summary>
        AssertNotNull(result);
        mockTestRunner.Verify(r => r.RunTestsAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task TestEmptyResults()
    {
        var mockTestRunner = new Mock<ITestRunner>();
        var mockLogger = new Mock<ILogger<RunTestsHandler>>();
        var handler = new RunTestsHandler(mockTestRunner.Object, mockLogger.Object);

        var expectedResult = new TestExecutionResult
        {
            TotalTests = 0,
            PassedTests = 0,
            FailedTests = 0,
            TotalDuration = TimeSpan.Zero,
            Results = new List<TestResult>()
        };

        mockTestRunner
            .Setup(r => r.RunTestsAsync(It.IsAny<string?>(), It.IsAny<IProgress<ProgressReport>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var command = new RunTestsCommand("NonExistentFilter");
        var result = await handler.Handle(command, CancellationToken.None);

        /// <summary>Assert not null.</summary>
        AssertNotNull(result);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, result.TotalTests);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, result.PassedTests);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, result.FailedTests);
        /// <summary>Assert equal.</summary>
        AssertEqual(0, result.Results.Count);
    }
}

