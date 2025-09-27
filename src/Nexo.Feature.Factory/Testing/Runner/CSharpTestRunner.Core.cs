using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Attributes;
using Nexo.Feature.Factory.Testing.Models;
using Nexo.Feature.Factory.Testing.Progress;
using Nexo.Feature.Factory.Testing.Coverage;
using Nexo.Feature.Factory.Testing.Timeout;

namespace Nexo.Feature.Factory.Testing.Runner
{
    /// <summary>
    /// Core C# test runner functionality
    /// </summary>
    public sealed partial class CSharpTestRunner : ITestRunner
    {
        /// <summary>
        /// Runs all tests with the specified configuration.
        /// </summary>
        public async Task<TestExecutionSummary> RunAllTestsAsync(TestConfiguration configuration, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting C# test runner execution");

            var filter = new TestFilter();
            return await RunFilteredTestsAsync(configuration, filter, cancellationToken);
        }

        /// <summary>
        /// Runs tests matching the specified filter.
        /// </summary>
        public async Task<TestExecutionSummary> RunFilteredTestsAsync(TestConfiguration configuration, TestFilter filter, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Running filtered tests with C# test runner");

            var startTime = DateTimeOffset.UtcNow;
            var results = new Dictionary<string, TestCommandResult>();
            var sharedData = new Dictionary<string, object>();

            try
            {
                // Discover tests if not already done
                if (!_discoveredTests.Any())
                {
                    await DiscoverTestsAsync(cancellationToken);
                }

                // Filter tests
                var testsToRun = _discoveredTests.Where(filter.Matches).ToList();
                _logger.LogInformation("Found {TestCount} tests matching filter", testsToRun.Count);

                // Report test execution start
                _progressReporter.ReportTestExecutionStart(testsToRun.Count);

                // Calculate overall timeout
                var totalEstimatedDuration = TimeSpan.FromTicks(testsToRun.Sum(t => t.EstimatedDuration.Ticks));
                var overallTimeout = TimeSpan.FromMinutes(Math.Max(10, totalEstimatedDuration.TotalMinutes * 2));
                _logger.LogInformation("Overall test execution timeout: {OverallTimeout}", overallTimeout);

                // Create overall timeout cancellation token
                using var overallTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                overallTimeoutCts.CancelAfter(overallTimeout);

                // Execute tests with progress reporting
                for (int i = 0; i < testsToRun.Count; i++)
                {
                    var testInfo = testsToRun[i];
                    
                    // Report test start
                    _progressReporter.ReportTestStart(testInfo.TestId, testInfo.DisplayName, i);

                    var result = await ExecuteTestAsync(testInfo, configuration, sharedData, overallTimeoutCts.Token);
                    results[testInfo.TestId] = result;

                    // Report test completion
                    _progressReporter.ReportTestComplete(testInfo.TestId, testInfo.DisplayName, 
                        result.ExecutionResult.IsSuccess, result.ExecutionResult.Duration, i);

                    // Report progress
                    var elapsed = DateTimeOffset.UtcNow - startTime;
                    var estimatedRemaining = i > 0 ? TimeSpan.FromTicks(elapsed.Ticks * (testsToRun.Count - i) / i) : TimeSpan.Zero;
                    _progressReporter.ReportProgress(i + 1, testsToRun.Count, elapsed, estimatedRemaining);

                    // Add test results to shared data for dependent tests
                    sharedData[testInfo.TestId] = result;

                    if (!result.ExecutionResult.IsSuccess && testInfo.Priority == TestPriority.Critical)
                    {
                        _logger.LogError("Critical test failed: {TestId}, stopping execution", testInfo.TestId);
                        _progressReporter.ReportError(testInfo.TestId, "Critical test failed, stopping execution");
                        break;
                    }
                }

                var endTime = DateTimeOffset.UtcNow;
                var summary = new TestExecutionSummary(
                    startTime,
                    endTime,
                    results,
                    sharedData
                );

                // Report test execution completion
                _progressReporter.ReportTestExecutionComplete(summary);

                // Analyze and report coverage if enabled
                if (configuration.EnablePerformanceMonitoring)
                {
                    await AnalyzeAndReportCoverageAsync(configuration, cancellationToken);
                }

                _logger.LogInformation("C# test execution completed: {SuccessCount}/{TotalCount} successful", 
                    summary.SuccessfulCommandCount, summary.TotalCommandCount);

                return summary;
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                var endTime = DateTimeOffset.UtcNow;
                var totalDuration = endTime - startTime;
                var isTimeout = totalDuration >= TimeSpan.FromMinutes(10);
                var errorMessage = isTimeout 
                    ? $"C# test execution timed out after {totalDuration.TotalMinutes:F1} minutes"
                    : "C# test execution was cancelled";
                
                _logger.LogError(ex, "C# test execution failed: {Error}", errorMessage);
                
                var summary = new TestExecutionSummary(
                    startTime,
                    endTime,
                    results,
                    sharedData,
                    errorMessage
                );

                _progressReporter.ReportTestExecutionComplete(summary);
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "C# test execution failed");
                var endTime = DateTimeOffset.UtcNow;
                
                var summary = new TestExecutionSummary(
                    startTime,
                    endTime,
                    results,
                    sharedData,
                    ex.Message
                );

                _progressReporter.ReportTestExecutionComplete(summary);
                return summary;
            }
        }
    }
}
