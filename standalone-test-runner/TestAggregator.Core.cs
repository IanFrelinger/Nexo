using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Core operations functionality for TestAggregator.
    /// Handles main test management, execution, and aggregation operations.
    /// </summary>
    public partial class TestAggregator
    {
        /// <summary>
        /// Adds a test to the aggregator's collection.
        /// </summary>
        public void AddTest(TestInfo test)
        {
            if (test == null)
                throw new ArgumentNullException(nameof(test));

            _tests.Add(test);
            
            if (_verbose)
            {
                Console.WriteLine($"Added test: {test.DisplayName} ({test.TestId})");
            }
        }

        /// <summary>
        /// Adds multiple tests to the aggregator's collection.
        /// </summary>
        public void AddTests(IEnumerable<TestInfo> tests)
        {
            if (tests == null)
                throw new ArgumentNullException(nameof(tests));

            foreach (var test in tests)
            {
                AddTest(test);
            }
        }

        /// <summary>
        /// Gets the current collection of tests.
        /// </summary>
        public IReadOnlyList<TestInfo> Tests => _tests.AsReadOnly();

        /// <summary>
        /// Gets the count of tests in the aggregator.
        /// </summary>
        public int TestCount => _tests.Count;

        /// <summary>
        /// Clears all tests from the aggregator.
        /// </summary>
        public void ClearTests()
        {
            _tests.Clear();
            
            if (_verbose)
            {
                Console.WriteLine("Cleared all tests from aggregator");
            }
        }

        /// <summary>
        /// Runs all tests in the aggregator's collection.
        /// </summary>
        public async Task<TestAggregationResult> RunAllTestsAsync(bool progress = false)
        {
            if (_tests.Count == 0)
            {
                throw new InvalidOperationException("No tests to run. Add tests to the aggregator first.");
            }

            var startTime = DateTimeOffset.UtcNow;
            var results = new List<TestResult>();
            var passedTests = 0;
            var failedTests = 0;
            var skippedTests = 0;

            if (_verbose)
            {
                Console.WriteLine($"Starting test aggregation with {_tests.Count} tests");
                Console.WriteLine($"Force timeout: {_forceTimeout}");
                Console.WriteLine($"Heartbeat interval: {_heartbeatInterval}s");
                Console.WriteLine($"Process timeout: {_processTimeout}m");
            }

            if (progress)
            {
                Console.WriteLine($"\nStats Test Aggregation: Starting {_tests.Count} tests...");
            }

            // Iterate through each test in the collection
            for (int i = 0; i < _tests.Count; i++)
            {
                var test = _tests[i];
                
                if (progress)
                {
                    Console.WriteLine($"\nProcessing [{i + 1}/{_tests.Count}] Aggregating: {test.DisplayName}");
                    Console.WriteLine($"   Category: {test.Category}, Priority: {test.Priority}");
                    Console.WriteLine($"   Timeout: {test.Timeout}s, Estimated: {test.EstimatedDuration}s");
                }

                try
                {
                    var result = await ExecuteTestAsync(test);
                    results.Add(result);

                    if (result.IsSuccess)
                    {
                        passedTests++;
                        if (progress)
                        {
                            Console.WriteLine($"   SUCCESS: {test.DisplayName} - PASSED ({result.Duration.TotalMilliseconds:F0}ms)");
                        }
                    }
                    else
                    {
                        failedTests++;
                        if (progress)
                        {
                            Console.WriteLine($"   ERROR: {test.DisplayName} - FAILED ({result.Duration.TotalMilliseconds:F0}ms): {result.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_verbose)
                    {
                        Console.WriteLine($"Test {test.TestId} failed with exception: {ex.Message}");
                    }

                    failedTests++;
                    results.Add(new TestResult(test.TestId, false, TimeSpan.Zero, ex.Message));

                    if (progress)
                    {
                        Console.WriteLine($"   ERROR: {test.DisplayName} - EXCEPTION: {ex.Message}");
                    }
                }

                // Add a small delay between tests for better visibility
                if (progress && i < _tests.Count - 1)
                {
                    await Task.Delay(100);
                }
            }

            var endTime = DateTimeOffset.UtcNow;
            var totalDuration = endTime - startTime;

            if (progress)
            {
                Console.WriteLine($"\nStats Test Aggregation: Completed {_tests.Count} tests in {totalDuration.TotalSeconds:F1}s");
            }

            var aggregationResult = new TestAggregationResult(
                _tests.Count,
                passedTests,
                failedTests,
                skippedTests,
                totalDuration,
                TimeSpan.FromTicks(results.Select(r => r.Duration.Ticks).Sum()),
                results.Select(r => r.Duration.TotalMilliseconds).DefaultIfEmpty().Average(),
                results,
                _tests,
                new TestAggregationMetrics(
                    _tests.Count,
                    passedTests,
                    failedTests,
                    skippedTests,
                    totalDuration,
                    results.Count(r => r.Duration.TotalMilliseconds > 1000), // Slow tests
                    results.Count(r => r.Duration.TotalMilliseconds < 500),  // Fast tests
                    _tests.GroupBy(t => t.Category).ToDictionary(g => g.Key, g => g.Count()),
                    _tests.GroupBy(t => t.Priority).ToDictionary(g => g.Key, g => g.Count())
                )
            );

            if (_verbose)
            {
                Console.WriteLine($"Test aggregation completed:");
                Console.WriteLine($"  Total: {aggregationResult.TotalTests}");
                Console.WriteLine($"  Passed: {aggregationResult.PassedTests}");
                Console.WriteLine($"  Failed: {aggregationResult.FailedTests}");
                Console.WriteLine($"  Skipped: {aggregationResult.SkippedTests}");
                Console.WriteLine($"  Duration: {aggregationResult.TotalDuration.TotalSeconds:F1}s");
            }

            return aggregationResult;
        }
    }
}
