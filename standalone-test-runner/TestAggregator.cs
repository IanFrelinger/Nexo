using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Test aggregator that takes a collection of tests and iterates through each one.
    /// Provides structured test execution with comprehensive reporting.
    /// </summary>
    public class TestAggregator
    {
        private readonly bool _forceTimeout;
        private readonly int _heartbeatInterval;
        private readonly int _processTimeout;
        private readonly bool _verbose;
        private readonly List<TestInfo> _tests;

        public TestAggregator(bool forceTimeout = false, int heartbeatInterval = 2, int processTimeout = 1, bool verbose = false)
        {
            _forceTimeout = forceTimeout;
            _heartbeatInterval = heartbeatInterval;
            _processTimeout = processTimeout;
            _verbose = verbose;
            _tests = new List<TestInfo>();
        }

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
        /// Discovers and adds default tests to the aggregator.
        /// </summary>
        public void DiscoverDefaultTests()
        {
            var defaultTests = new List<TestInfo>
            {
                new TestInfo(
                    "aggregator-basic-validation",
                    "Basic Validation Test",
                    "Simple test that validates basic functionality",
                    "Unit",
                    "High",
                    2,
                    5,
                    new[] { "aggregator", "basic", "validation" }
                ),
                new TestInfo(
                    "aggregator-configuration-test",
                    "Configuration Test",
                    "Simple test that validates configuration loading",
                    "Unit",
                    "Medium",
                    1,
                    3,
                    new[] { "aggregator", "configuration" }
                ),
                new TestInfo(
                    "aggregator-timeout-test",
                    "Timeout Test",
                    "Simple test that validates timeout handling",
                    "Unit",
                    "High",
                    1,
                    3,
                    new[] { "aggregator", "timeout" }
                ),
                new TestInfo(
                    "aggregator-performance-test",
                    "Performance Test",
                    "Simple test that validates performance",
                    "Performance",
                    "Medium",
                    3,
                    8,
                    new[] { "aggregator", "performance" }
                ),
                new TestInfo(
                    "aggregator-integration-test",
                    "Integration Test",
                    "Test that validates component integration",
                    "Integration",
                    "High",
                    2,
                    6,
                    new[] { "aggregator", "integration" }
                ),
                new TestInfo(
                    "aggregator-security-test",
                    "Security Test",
                    "Test that validates security measures",
                    "Security",
                    "Critical",
                    1,
                    4,
                    new[] { "aggregator", "security" }
                )
            };

            AddTests(defaultTests);
            
            if (_verbose)
            {
                Console.WriteLine($"Discovered and added {defaultTests.Count} default tests");
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
                Console.WriteLine($"\n📊 Test Aggregation: Starting {_tests.Count} tests...");
            }

            // Iterate through each test in the collection
            for (int i = 0; i < _tests.Count; i++)
            {
                var test = _tests[i];
                
                if (progress)
                {
                    Console.WriteLine($"\n🔄 [{i + 1}/{_tests.Count}] Aggregating: {test.DisplayName}");
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
                            Console.WriteLine($"   ✅ {test.DisplayName} - PASSED ({result.Duration.TotalMilliseconds:F0}ms)");
                        }
                    }
                    else
                    {
                        failedTests++;
                        if (progress)
                        {
                            Console.WriteLine($"   ❌ {test.DisplayName} - FAILED ({result.Duration.TotalMilliseconds:F0}ms): {result.ErrorMessage}");
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
                        Console.WriteLine($"   ❌ {test.DisplayName} - EXCEPTION: {ex.Message}");
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
                Console.WriteLine($"\n📊 Test Aggregation: Completed {_tests.Count} tests in {totalDuration.TotalSeconds:F1}s");
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

        /// <summary>
        /// Runs tests filtered by category.
        /// </summary>
        public async Task<TestAggregationResult> RunTestsByCategoryAsync(string category, bool progress = false)
        {
            var filteredTests = _tests.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (filteredTests.Count == 0)
            {
                throw new InvalidOperationException($"No tests found for category: {category}");
            }

            if (_verbose)
            {
                Console.WriteLine($"Running {filteredTests.Count} tests for category: {category}");
            }

            // Temporarily replace tests with filtered ones
            var originalTests = new List<TestInfo>(_tests);
            _tests.Clear();
            _tests.AddRange(filteredTests);

            try
            {
                return await RunAllTestsAsync(progress);
            }
            finally
            {
                // Restore original tests
                _tests.Clear();
                _tests.AddRange(originalTests);
            }
        }

        /// <summary>
        /// Runs tests filtered by priority.
        /// </summary>
        public async Task<TestAggregationResult> RunTestsByPriorityAsync(string priority, bool progress = false)
        {
            var filteredTests = _tests.Where(t => t.Priority.Equals(priority, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (filteredTests.Count == 0)
            {
                throw new InvalidOperationException($"No tests found for priority: {priority}");
            }

            if (_verbose)
            {
                Console.WriteLine($"Running {filteredTests.Count} tests for priority: {priority}");
            }

            // Temporarily replace tests with filtered ones
            var originalTests = new List<TestInfo>(_tests);
            _tests.Clear();
            _tests.AddRange(filteredTests);

            try
            {
                return await RunAllTestsAsync(progress);
            }
            finally
            {
                // Restore original tests
                _tests.Clear();
                _tests.AddRange(originalTests);
            }
        }

        /// <summary>
        /// Executes a single test with timeout protection.
        /// </summary>
        private async Task<TestResult> ExecuteTestAsync(TestInfo testInfo)
        {
            var startTime = DateTimeOffset.UtcNow;

            try
            {
                if (_verbose)
                {
                    Console.WriteLine($"Executing test: {testInfo.TestId} with timeout: {testInfo.Timeout}s");
                }

                // Create aggressive timeout if enabled
                using var timeoutCts = new CancellationTokenSource();
                if (_forceTimeout)
                {
                    // Use more aggressive timeout
                    var aggressiveTimeout = Math.Min(testInfo.Timeout, 3); // Cap at 3 seconds for aggressive mode
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(aggressiveTimeout));
                }
                else
                {
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(testInfo.Timeout));
                }

                // Execute the test method with timeout monitoring
                var testExecutionTask = Task.Run(() => InvokeTestMethod(testInfo.TestId), timeoutCts.Token);

                try
                {
                    var result = await testExecutionTask;
                    var endTime = DateTimeOffset.UtcNow;
                    var duration = endTime - startTime;

                    if (_verbose)
                    {
                        Console.WriteLine($"Test {testInfo.TestId} completed: {result} ({duration.TotalMilliseconds}ms)");
                    }

                    return new TestResult(testInfo.TestId, result, duration, null);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    var endTime = DateTimeOffset.UtcNow;
                    var duration = endTime - startTime;

                    if (_verbose)
                    {
                        Console.WriteLine($"Test {testInfo.TestId} timed out after {duration.TotalMilliseconds}ms");
                    }

                    return new TestResult(testInfo.TestId, false, duration,
                        $"Test timed out after {testInfo.Timeout} seconds");
                }
            }
            catch (Exception ex)
            {
                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;

                if (_verbose)
                {
                    Console.WriteLine($"Test {testInfo.TestId} failed: {ex.Message}");
                }

                return new TestResult(testInfo.TestId, false, duration, ex.Message);
            }
        }

        /// <summary>
        /// Invokes the appropriate test method based on test ID.
        /// </summary>
        private bool InvokeTestMethod(string testId)
        {
            return testId switch
            {
                "aggregator-basic-validation" => RunBasicValidationTest(),
                "aggregator-configuration-test" => RunConfigurationTest(),
                "aggregator-timeout-test" => RunTimeoutTest(),
                "aggregator-performance-test" => RunPerformanceTest(),
                "aggregator-integration-test" => RunIntegrationTest(),
                "aggregator-security-test" => RunSecurityTest(),
                _ when testId.StartsWith("large-test-") => RunGenericTest(), // Handle large test collection
                _ when testId.StartsWith("smoke-") => RunGenericTest(), // Handle smoke tests
                _ => RunGenericTest() // Default fallback for any unknown test
            };
        }

        // Test method implementations
        private bool RunBasicValidationTest()
        {
            Thread.Sleep(1000); // Simulate some work
            return true;
        }

        private bool RunConfigurationTest()
        {
            Thread.Sleep(500); // Simulate some work
            return true;
        }

        private bool RunTimeoutTest()
        {
            Thread.Sleep(2000); // Simulate some work
            return true;
        }

        private bool RunPerformanceTest()
        {
            Thread.Sleep(3000); // Simulate some work
            return true;
        }

        private bool RunIntegrationTest()
        {
            Thread.Sleep(1500); // Simulate some work
            return true;
        }

        private bool RunSecurityTest()
        {
            Thread.Sleep(800); // Simulate some work
            return true;
        }

        private bool RunGenericTest()
        {
            Thread.Sleep(500); // Simulate some work for generic tests
            return true;
        }
    }

    /// <summary>
    /// Result of test aggregation execution.
    /// </summary>
    public record TestAggregationResult(
        int TotalTests,
        int PassedTests,
        int FailedTests,
        int SkippedTests,
        TimeSpan TotalDuration,
        TimeSpan TotalExecutionTime,
        double AverageDuration,
        List<TestResult> TestResults,
        List<TestInfo> Tests,
        TestAggregationMetrics Metrics
    );

    /// <summary>
    /// Metrics for test aggregation.
    /// </summary>
    public record TestAggregationMetrics(
        int TotalTests,
        int PassedTests,
        int FailedTests,
        int SkippedTests,
        TimeSpan TotalDuration,
        int SlowTests,
        int FastTests,
        Dictionary<string, int> TestsByCategory,
        Dictionary<string, int> TestsByPriority
    );
}
