using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using TestingTestResult = Nexo.Core.Application.Testing.Models.TestResult;
using Nexo.Core.Application.Testing.Ports;
using Nexo.Infrastructure.Testing;
using Nexo.Tests.Application.Helpers;

namespace Nexo.Tests.Infrastructure.Tests.Testing;

// Simple test class for testing TestRunnerAdapter
public class SimpleTestForRunner : UnitTestBase
{
    public override Task<TestingTestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestingTestResult
        {
            TestName = nameof(SimpleTestForRunner),
            Category = "TestRunner",
            Passed = true,
            Message = "Simple test for TestRunnerAdapter"
        });
    }
}

public class TestRunnerAdapterTests : UnitTestBase
{
    public override async Task<TestingTestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestTestDiscovery();
            await TestTestDiscoveryWithFilter();
            await TestTestExecution();
            await TestProgressReporting();
            await TestCancellation();
            await TestExceptionHandling();

            return new TestingTestResult
            {
                TestName = nameof(TestRunnerAdapterTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All TestRunnerAdapter tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestingTestResult
            {
                TestName = nameof(TestRunnerAdapterTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestingTestResult
            {
                TestName = nameof(TestRunnerAdapterTests),
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
        var progress = new Progress<ProgressReport>(report => progressReports.Add(report));

        await runner.RunTestsAsync("SimpleTestForRunner", progress, CancellationToken.None);

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

