using Ashlar.Tests.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ashlar.Core.Application.Common.Models;
using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Ports;
using Ashlar.Infrastructure.Testing;
using Ashlar.Tests.Application.Helpers;

namespace Ashlar.Tests.Infrastructure.Tests.Testing;

public class TestRunnerAdapterTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestTestDiscovery();
            await TestTestDiscoveryWithFilter();
            await TestTestExecution();
            await TestProgressReporting();
            await TestCancellation();
            await TestExceptionHandling();

            return new TestResult
            {
                Name = nameof(TestRunnerAdapterTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All TestRunnerAdapter tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(TestRunnerAdapterTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(TestRunnerAdapterTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestTestDiscovery()
    {
        var mockLogger = new Mock<ILogger<TestRunnerAdapter>>();
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var runner = new TestRunnerAdapter(mockLogger.Object, serviceProvider);

        // Filter to only discover SimpleTestForRunner to avoid recursion
        // (TestRunnerAdapterTests would discover itself and cause infinite loop)
        var result = await runner.RunTestsAsync("SimpleTestForRunner", null, CancellationToken.None);

        AssertNotNull(result);
        AssertTrue(result.TotalTests >= 1, "Should discover at least SimpleTestForRunner");
        AssertTrue(result.Results.Count >= 1, "Should have test results");
        AssertTrue(result.Results.Any(r => r.TestName.Contains("SimpleTestForRunner", StringComparison.OrdinalIgnoreCase)),
            "Should discover SimpleTestForRunner");
    }

    private async Task TestTestDiscoveryWithFilter()
    {
        var mockLogger = new Mock<ILogger<TestRunnerAdapter>>();
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var runner = new TestRunnerAdapter(mockLogger.Object, serviceProvider);

        // Filter by test name
        var result = await runner.RunTestsAsync("SimpleTestForRunner", null, CancellationToken.None);

        AssertNotNull(result);
        // Should find at least SimpleTestForRunner
        AssertTrue(result.TotalTests >= 1, "Should find at least one test with filter");
        AssertTrue(result.Results.Any(r => r.TestName.Contains("SimpleTestForRunner", StringComparison.OrdinalIgnoreCase)),
            "Should find SimpleTestForRunner test");
    }

    private async Task TestTestExecution()
    {
        var mockLogger = new Mock<ILogger<TestRunnerAdapter>>();
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var runner = new TestRunnerAdapter(mockLogger.Object, serviceProvider);

        // Execute SimpleTestForRunner which should pass
        var result = await runner.RunTestsAsync("SimpleTestForRunner", null, CancellationToken.None);

        AssertNotNull(result);
        AssertTrue(result.TotalTests >= 1, "Should have discovered SimpleTestForRunner");
        var simpleTest = result.Results.FirstOrDefault(r => r.TestName.Contains("SimpleTestForRunner", StringComparison.OrdinalIgnoreCase));
        AssertNotNull(simpleTest, "Should have found SimpleTestForRunner result");
        AssertTrue(simpleTest!.Passed, "SimpleTestForRunner should pass");
    }

    private async Task TestProgressReporting()
    {
        var mockLogger = new Mock<ILogger<TestRunnerAdapter>>();
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var runner = new TestRunnerAdapter(mockLogger.Object, serviceProvider);

        var progressReports = new List<ProgressReport>();
        var progress = new SyncProgress<ProgressReport>(report => progressReports.Add(report));

        await runner.RunTestsAsync("SimpleTestForRunner", progress, CancellationToken.None);

        // Progress<T> may post callbacks asynchronously; allow the queue to drain before asserting.
        await Task.Delay(300, CancellationToken.None).ConfigureAwait(false);

        // Should have progress reports
        AssertTrue(progressReports.Count > 0, "Should have progress reports");
        AssertTrue(progressReports.Any(r => r.Message.Contains("Discovering", StringComparison.OrdinalIgnoreCase) ||
                                           r.Message.Contains("Found", StringComparison.OrdinalIgnoreCase) ||
                                           r.Message.Contains("Running", StringComparison.OrdinalIgnoreCase) ||
                                           r.Message.Contains("completed", StringComparison.OrdinalIgnoreCase)),
            "Should have progress reports about discovery and execution");
    }

    private async Task TestCancellation()
    {
        var mockLogger = new Mock<ILogger<TestRunnerAdapter>>();
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var runner = new TestRunnerAdapter(mockLogger.Object, serviceProvider);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should handle cancellation gracefully
        await AssertThrowsAsync<OperationCanceledException>(() =>
            runner.RunTestsAsync(null, null, cts.Token));
    }

    private async Task TestExceptionHandling()
    {
        var mockLogger = new Mock<ILogger<TestRunnerAdapter>>();
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var runner = new TestRunnerAdapter(mockLogger.Object, serviceProvider);

        // Run SimpleTestForRunner - if it throws an exception, it should be caught and reported
        // We filter to avoid recursion from TestRunnerAdapterTests discovering itself
        var result = await runner.RunTestsAsync("SimpleTestForRunner", null, CancellationToken.None);

        AssertNotNull(result);
        // Should have results even if some tests failed
        AssertTrue(result.Results.Count > 0, "Should have test results");
        // Each result should have a TestName and Category
        AssertTrue(result.Results.All(r => !string.IsNullOrEmpty(r.TestName)),
            "All test results should have a TestName");
        AssertTrue(result.Results.All(r => !string.IsNullOrEmpty(r.Category)),
            "All test results should have a Category");
    }
}
