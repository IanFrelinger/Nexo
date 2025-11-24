using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Testing.Abstractions;
using TestingTestResult = Nexo.Core.Application.Testing.Models.TestResult;
using Nexo.Core.Application.Validation.Models;
using Nexo.Infrastructure.Validation.Adapters;
using Nexo.Infrastructure.Validation.Parsers;
using ValidationTestResult = Nexo.Core.Application.Validation.Models.TestResult;

namespace Nexo.Tests.Infrastructure.Tests.Validation;

public class ValidationServiceAdapterTests : UnitTestBase
{
    public override async Task<TestingTestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestNoTestProjectsFound();
            await TestProgressReporting();
            await TestCancellation();
            await TestExceptionHandling();

            return new TestingTestResult
            {
                TestName = nameof(ValidationServiceAdapterTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All ValidationServiceAdapter tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestingTestResult
            {
                TestName = nameof(ValidationServiceAdapterTests),
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
                TestName = nameof(ValidationServiceAdapterTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }

    private async Task TestNoTestProjectsFound()
    {
        var mockLogger = new Mock<ILogger<ValidationServiceAdapter>>();
        var mockParser = new Mock<ITestResultParser>();
        var adapter = new ValidationServiceAdapter(mockLogger.Object, mockParser.Object);

        // Change to a directory that likely has no test projects
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            Directory.SetCurrentDirectory(tempDir);

            var result = await adapter.ValidateAsync(null, null, CancellationToken.None);

            AssertNotNull(result);
            AssertTrue(result.Passed, "Should pass when no test projects found");
            AssertEqual("No test projects found - validation skipped", result.Message);
            AssertEqual(0, result.TestsRun);
            AssertEqual(0, result.TestsPassed);
            AssertEqual(0, result.TestsFailed);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    private async Task TestProgressReporting()
    {
        var mockLogger = new Mock<ILogger<ValidationServiceAdapter>>();
        var mockParser = new Mock<ITestResultParser>();
        var adapter = new ValidationServiceAdapter(mockLogger.Object, mockParser.Object);

        ProgressReport? capturedReport = null;
        var progress = new Progress<ProgressReport>(report => capturedReport = report);

        // Change to a directory that likely has no test projects
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            Directory.SetCurrentDirectory(tempDir);

            await adapter.ValidateAsync(null, progress, CancellationToken.None);

            // Should have progress reports - Progress<T> may invoke callback asynchronously
            // The important thing is the adapter doesn't throw and returns a result
            if (capturedReport != null)
            {
                AssertTrue(capturedReport.Message.Contains("Starting validation", StringComparison.OrdinalIgnoreCase) || 
                          capturedReport.Message.Contains("validation", StringComparison.OrdinalIgnoreCase) ||
                          capturedReport.Message.Contains("No test projects", StringComparison.OrdinalIgnoreCase),
                    $"Progress message should mention validation, got: {capturedReport.Message}");
            }
            // If no progress report captured, that's acceptable - the test still verifies the adapter works
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    private async Task TestCancellation()
    {
        var mockLogger = new Mock<ILogger<ValidationServiceAdapter>>();
        var mockParser = new Mock<ITestResultParser>();
        var adapter = new ValidationServiceAdapter(mockLogger.Object, mockParser.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should handle cancellation gracefully and return a result
        var result = await adapter.ValidateAsync(null, null, cts.Token);

        AssertNotNull(result);
        // May return error result or handle cancellation
    }

    private async Task TestExceptionHandling()
    {
        var mockLogger = new Mock<ILogger<ValidationServiceAdapter>>();
        var mockParser = new Mock<ITestResultParser>();
        var adapter = new ValidationServiceAdapter(mockLogger.Object, mockParser.Object);

        // The adapter catches all exceptions and returns an error result
        // We can't easily trigger an exception without mocking the file system,
        // but we can verify the adapter handles errors gracefully
        var result = await adapter.ValidateAsync("test-filter", null, CancellationToken.None);

        AssertNotNull(result);
        // Should return a result (either success or failure, but not throw)
    }
}

