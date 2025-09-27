using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Models;
using Nexo.Feature.Factory.Testing.Progress;
using Nexo.Feature.Factory.Testing.Timeout;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Test runner functionality for simple testing commands.
    /// </summary>
    public static partial class SimpleTestingCommands
    {
        /// <summary>
        /// Simple test runner that runs basic tests without complex AI dependencies.
        /// </summary>
        public sealed class SimpleTestRunner
        {
            private readonly ILogger<SimpleTestRunner> _logger;
            private readonly IServiceProvider _serviceProvider;
            private readonly IProgressReporter _progressReporter;
            private readonly ITimeoutManager _timeoutManager;

            public SimpleTestRunner(
                ILogger<SimpleTestRunner> logger, 
                IServiceProvider serviceProvider,
                IProgressReporter? progressReporter = null,
                ITimeoutManager? timeoutManager = null)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                _progressReporter = progressReporter ?? new ConsoleProgressReporter(
                    serviceProvider.GetRequiredService<ILogger<ConsoleProgressReporter>>());
                _timeoutManager = timeoutManager ?? new AggressiveTimeoutManager(
                    serviceProvider.GetRequiredService<ILogger<AggressiveTimeoutManager>>());
            }

            public async Task<IEnumerable<SimpleTestInfo>> DiscoverTestsAsync()
            {
                var tests = new List<SimpleTestInfo>
                {
                    new SimpleTestInfo(
                        "simple-basic-validation",
                        "Basic Validation Test",
                        "Simple test that validates basic functionality",
                        "Unit",
                        "High",
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(5),
                        new[] { "simple", "basic", "validation" }
                    ),
                    new SimpleTestInfo(
                        "simple-configuration-test",
                        "Configuration Test",
                        "Simple test that validates configuration loading",
                        "Unit",
                        "Medium",
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(3),
                        new[] { "simple", "configuration" }
                    ),
                    new SimpleTestInfo(
                        "simple-timeout-test",
                        "Timeout Test",
                        "Simple test that validates timeout handling",
                        "Unit",
                        "High",
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(3),
                        new[] { "simple", "timeout" }
                    )
                };

                _logger.LogInformation("Discovered {Count} simple tests", tests.Count);
                return await Task.FromResult(tests);
            }

            public async Task<SimpleTestSummary> RunAllTestsAsync(TestConfiguration configuration, CancellationToken cancellationToken)
            {
                var tests = await DiscoverTestsAsync();
                var startTime = DateTimeOffset.UtcNow;

                _logger.LogInformation("Running {Count} simple tests with aggressive timeout protection", tests.Count());
                _progressReporter.ReportTestExecutionStart(tests.Count());

                var results = new List<SimpleTestResult>();
                var passedTests = 0;
                var failedTests = 0;

                int i = 0;
                foreach (var test in tests)
                {
                    _progressReporter.ReportTestStart(test.TestId, test.DisplayName, i);

                    try
                    {
                        var result = await ExecuteSimpleTestAsync(test, configuration, cancellationToken);
                        results.Add(result);
                        
                        if (result.IsSuccess)
                        {
                            passedTests++;
                            _progressReporter.ReportTestComplete(test.TestId, test.DisplayName, true, result.Duration, i);
                        }
                        else
                        {
                            failedTests++;
                            _progressReporter.ReportTestComplete(test.TestId, test.DisplayName, false, result.Duration, i);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Test {TestId} failed with exception", test.TestId);
                        _progressReporter.ReportError(test.TestId, ex.Message);
                        
                        failedTests++;
                        results.Add(new SimpleTestResult(test.TestId, false, TimeSpan.Zero, ex.Message));
                    }

                    // Report progress
                    var elapsed = DateTimeOffset.UtcNow - startTime;
                    var estimatedRemaining = TimeSpan.FromTicks(elapsed.Ticks * (tests.Count() - i - 1) / (i + 1));
                    _progressReporter.ReportProgress(i + 1, tests.Count(), elapsed, estimatedRemaining);
                    
                    i++;
                }

                var endTime = DateTimeOffset.UtcNow;
                var totalDuration = endTime - startTime;

                var summary = new SimpleTestSummary(
                    results.Count,
                    passedTests,
                    failedTests,
                    totalDuration,
                    TimeSpan.FromTicks(results.Select(r => r.Duration.Ticks).Sum()),
                    results.Any() ? results.Select(r => r.Duration.TotalMilliseconds).Average() : 0.0,
                    results.Where(r => !r.IsSuccess).Select(r => r.ErrorMessage).Where(m => !string.IsNullOrEmpty(m)).Cast<string>().ToList()
                );

                var commandResults = new Dictionary<string, TestCommandResult>();
                var sharedData = new Dictionary<string, object>
                {
                    ["totalTests"] = summary.TotalTests,
                    ["passedTests"] = summary.PassedTests,
                    ["failedTests"] = summary.FailedTests,
                    ["totalDuration"] = summary.TotalDuration,
                    ["averageDuration"] = summary.AverageDuration,
                    ["errorMessages"] = summary.ErrorMessages
                };
                
                _progressReporter.ReportTestExecutionComplete(new TestExecutionSummary(
                    DateTimeOffset.UtcNow - summary.TotalDuration,
                    DateTimeOffset.UtcNow,
                    commandResults,
                    sharedData
                ));

                return summary;
            }
        }
    }
}
