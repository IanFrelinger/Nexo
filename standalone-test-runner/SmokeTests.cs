using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Automated Smoke Tests for Test Aggregator Demo
    /// Ensures all functionality works reliably in different scenarios
    /// </summary>
    public class SmokeTests
    {
        private readonly List<SmokeTestResult> _results = new();
        private int _totalTests = 0;
        private int _passedTests = 0;
        private int _failedTests = 0;

        public async Task RunAllSmokeTestsAsync()
        {
            Console.WriteLine("🔥 Automated Smoke Tests for Test Aggregator");
            Console.WriteLine("===========================================");
            Console.WriteLine();

            var stopwatch = Stopwatch.StartNew();

            // Test Suite 1: Basic Functionality
            await RunBasicFunctionalityTests();

            // Test Suite 2: Filtering Capabilities
            await RunFilteringTests();

            // Test Suite 3: Configuration Options
            await RunConfigurationTests();

            // Test Suite 4: Error Handling
            await RunErrorHandlingTests();

            // Test Suite 5: Performance & Stress Tests
            await RunPerformanceTests();

            // Test Suite 6: Integration Tests
            await RunIntegrationTests();

            stopwatch.Stop();

            // Generate Smoke Test Report
            GenerateSmokeTestReport(stopwatch.Elapsed);
        }

        private async Task RunBasicFunctionalityTests()
        {
            Console.WriteLine("🧪 Test Suite 1: Basic Functionality");
            Console.WriteLine("-----------------------------------");

            // Test 1.1: Basic Test Discovery
            await RunSmokeTest("Basic Discovery", async () =>
            {
                var aggregator = new TestAggregator();
                aggregator.DiscoverDefaultTests();
                
                if (aggregator.TestCount != 6)
                    throw new Exception($"Expected 6 tests, got {aggregator.TestCount}");
                
                var expectedTestIds = new[] { "aggregator-basic-validation", "aggregator-configuration-test", 
                    "aggregator-timeout-test", "aggregator-performance-test", "aggregator-integration-test", 
                    "aggregator-security-test" };
                
                foreach (var expectedId in expectedTestIds)
                {
                    if (!aggregator.Tests.Any(t => t.TestId == expectedId))
                        throw new Exception($"Missing expected test: {expectedId}");
                }
            });

            // Test 1.2: Basic Test Execution
            await RunSmokeTest("Basic Test Execution", async () =>
            {
                var aggregator = new TestAggregator();
                aggregator.DiscoverDefaultTests();
                
                var result = await aggregator.RunAllTestsAsync();
                
                if (result.TotalTests != 6)
                    throw new Exception($"Expected 6 total tests, got {result.TotalTests}");
                
                if (result.PassedTests != 6)
                    throw new Exception($"Expected 6 passed tests, got {result.PassedTests}");
                
                if (result.FailedTests != 0)
                    throw new Exception($"Expected 0 failed tests, got {result.FailedTests}");
                
                if (result.TotalDuration.TotalSeconds < 5)
                    throw new Exception($"Expected duration >= 5s, got {result.TotalDuration.TotalSeconds:F1}s");
            });

            // Test 1.3: Test Collection Management
            await RunSmokeTest("Test Collection Management", async () =>
            {
                var aggregator = new TestAggregator();
                
                // Test empty state
                if (aggregator.TestCount != 0)
                    throw new Exception($"Expected 0 tests initially, got {aggregator.TestCount}");
                
                // Add single test
                var test = new TestInfo("smoke-test", "Smoke Test", "Description", "Unit", "High", 5, 2, new[] { "smoke" });
                aggregator.AddTest(test);
                
                if (aggregator.TestCount != 1)
                    throw new Exception($"Expected 1 test after add, got {aggregator.TestCount}");
                
                // Add multiple tests
                var tests = new List<TestInfo> 
                { 
                    new TestInfo("test-2", "Test 2", "Description", "Unit", "High", 5, 2, new[] { "test2" }),
                    new TestInfo("test-3", "Test 3", "Description", "Unit", "High", 5, 2, new[] { "test3" })
                };
                aggregator.AddTests(tests);
                
                if (aggregator.TestCount != 3)
                    throw new Exception($"Expected 3 tests after adding multiple, got {aggregator.TestCount}");
                
                // Clear tests
                aggregator.ClearTests();
                
                if (aggregator.TestCount != 0)
                    throw new Exception($"Expected 0 tests after clear, got {aggregator.TestCount}");
            });

            Console.WriteLine();
        }

        private async Task RunFilteringTests()
        {
            Console.WriteLine("🧪 Test Suite 2: Filtering Capabilities");
            Console.WriteLine("--------------------------------------");

            // Test 2.1: Category Filtering
            await RunSmokeTest("Category Filtering", async () =>
            {
                var aggregator = new TestAggregator();
                aggregator.DiscoverDefaultTests();
                
                // Test Unit category
                var unitResult = await aggregator.RunTestsByCategoryAsync("Unit");
                if (unitResult.TotalTests != 3)
                    throw new Exception($"Expected 3 Unit tests, got {unitResult.TotalTests}");
                
                // Test Performance category
                var performanceResult = await aggregator.RunTestsByCategoryAsync("Performance");
                if (performanceResult.TotalTests != 1)
                    throw new Exception($"Expected 1 Performance test, got {performanceResult.TotalTests}");
                
                // Test Integration category
                var integrationResult = await aggregator.RunTestsByCategoryAsync("Integration");
                if (integrationResult.TotalTests != 1)
                    throw new Exception($"Expected 1 Integration test, got {integrationResult.TotalTests}");
                
                // Test Security category
                var securityResult = await aggregator.RunTestsByCategoryAsync("Security");
                if (securityResult.TotalTests != 1)
                    throw new Exception($"Expected 1 Security test, got {securityResult.TotalTests}");
            });

            // Test 2.2: Priority Filtering
            await RunSmokeTest("Priority Filtering", async () =>
            {
                var aggregator = new TestAggregator();
                aggregator.DiscoverDefaultTests();
                
                // Test High priority
                var highResult = await aggregator.RunTestsByPriorityAsync("High");
                if (highResult.TotalTests != 3)
                    throw new Exception($"Expected 3 High priority tests, got {highResult.TotalTests}");
                
                // Test Medium priority
                var mediumResult = await aggregator.RunTestsByPriorityAsync("Medium");
                if (mediumResult.TotalTests != 2)
                    throw new Exception($"Expected 2 Medium priority tests, got {mediumResult.TotalTests}");
                
                // Test Critical priority
                var criticalResult = await aggregator.RunTestsByPriorityAsync("Critical");
                if (criticalResult.TotalTests != 1)
                    throw new Exception($"Expected 1 Critical priority test, got {criticalResult.TotalTests}");
            });

            // Test 2.3: Combined Filtering
            await RunSmokeTest("Combined Filtering", async () =>
            {
                var aggregator = new TestAggregator();
                aggregator.DiscoverDefaultTests();
                
                // Run Unit tests with progress
                var unitResult = await aggregator.RunTestsByCategoryAsync("Unit", progress: true);
                if (unitResult.TotalTests != 3)
                    throw new Exception($"Expected 3 Unit tests with progress, got {unitResult.TotalTests}");
                
                // Run High priority tests with progress
                var highResult = await aggregator.RunTestsByPriorityAsync("High", progress: true);
                if (highResult.TotalTests != 3)
                    throw new Exception($"Expected 3 High priority tests with progress, got {highResult.TotalTests}");
            });

            Console.WriteLine();
        }

        private async Task RunConfigurationTests()
        {
            Console.WriteLine("🧪 Test Suite 3: Configuration Options");
            Console.WriteLine("-------------------------------------");

            // Test 3.1: Force Timeout Configuration
            await RunSmokeTest("Force Timeout Configuration", async () =>
            {
                var aggregator = new TestAggregator(forceTimeout: true);
                aggregator.DiscoverDefaultTests();
                
                var result = await aggregator.RunAllTestsAsync();
                
                if (result.TotalTests != 6)
                    throw new Exception($"Force timeout: Expected 6 tests, got {result.TotalTests}");
                
                if (result.PassedTests != 6)
                    throw new Exception($"Force timeout: Expected 6 passed tests, got {result.PassedTests}");
            });

            // Test 3.2: Verbose Logging Configuration
            await RunSmokeTest("Verbose Logging Configuration", async () =>
            {
                var aggregator = new TestAggregator(verbose: true);
                aggregator.DiscoverDefaultTests();
                
                var result = await aggregator.RunAllTestsAsync();
                
                if (result.TotalTests != 6)
                    throw new Exception($"Verbose logging: Expected 6 tests, got {result.TotalTests}");
                
                if (result.PassedTests != 6)
                    throw new Exception($"Verbose logging: Expected 6 passed tests, got {result.PassedTests}");
            });

            // Test 3.3: Custom Timeout Configuration
            await RunSmokeTest("Custom Timeout Configuration", async () =>
            {
                var aggregator = new TestAggregator(heartbeatInterval: 1, processTimeout: 2);
                aggregator.DiscoverDefaultTests();
                
                var result = await aggregator.RunAllTestsAsync();
                
                if (result.TotalTests != 6)
                    throw new Exception($"Custom timeout: Expected 6 tests, got {result.TotalTests}");
                
                if (result.PassedTests != 6)
                    throw new Exception($"Custom timeout: Expected 6 passed tests, got {result.PassedTests}");
            });

            // Test 3.4: All Options Combined
            await RunSmokeTest("All Configuration Options Combined", async () =>
            {
                var aggregator = new TestAggregator(forceTimeout: true, heartbeatInterval: 1, processTimeout: 2, verbose: true);
                aggregator.DiscoverDefaultTests();
                
                var result = await aggregator.RunAllTestsAsync(progress: true);
                
                if (result.TotalTests != 6)
                    throw new Exception($"All options: Expected 6 tests, got {result.TotalTests}");
                
                if (result.PassedTests != 6)
                    throw new Exception($"All options: Expected 6 passed tests, got {result.PassedTests}");
            });

            Console.WriteLine();
        }

        private async Task RunErrorHandlingTests()
        {
            Console.WriteLine("🧪 Test Suite 4: Error Handling");
            Console.WriteLine("-------------------------------");

            // Test 4.1: Empty Collection Handling
            await RunSmokeTest("Empty Collection Handling", async () =>
            {
                var aggregator = new TestAggregator();
                
                try
                {
                    await aggregator.RunAllTestsAsync();
                    throw new Exception("Expected InvalidOperationException for empty collection");
                }
                catch (InvalidOperationException)
                {
                    // Expected behavior
                }
            });

            // Test 4.2: Invalid Category Handling
            await RunSmokeTest("Invalid Category Handling", async () =>
            {
                var aggregator = new TestAggregator();
                aggregator.DiscoverDefaultTests();
                
                try
                {
                    await aggregator.RunTestsByCategoryAsync("NonExistentCategory");
                    throw new Exception("Expected InvalidOperationException for invalid category");
                }
                catch (InvalidOperationException)
                {
                    // Expected behavior
                }
            });

            // Test 4.3: Invalid Priority Handling
            await RunSmokeTest("Invalid Priority Handling", async () =>
            {
                var aggregator = new TestAggregator();
                aggregator.DiscoverDefaultTests();
                
                try
                {
                    await aggregator.RunTestsByPriorityAsync("NonExistentPriority");
                    throw new Exception("Expected InvalidOperationException for invalid priority");
                }
                catch (InvalidOperationException)
                {
                    // Expected behavior
                }
            });

            // Test 4.4: Null Parameter Handling
            await RunSmokeTest("Null Parameter Handling", async () =>
            {
                var aggregator = new TestAggregator();
                
                try
                {
                    aggregator.AddTest(null!);
                    throw new Exception("Expected ArgumentNullException for null test");
                }
                catch (ArgumentNullException)
                {
                    // Expected behavior
                }
                
                try
                {
                    aggregator.AddTests(null!);
                    throw new Exception("Expected ArgumentNullException for null tests");
                }
                catch (ArgumentNullException)
                {
                    // Expected behavior
                }
            });

            Console.WriteLine();
        }

        private async Task RunPerformanceTests()
        {
            Console.WriteLine("🧪 Test Suite 5: Performance & Stress Tests");
            Console.WriteLine("-------------------------------------------");

            // Test 5.1: Multiple Concurrent Executions
            await RunSmokeTest("Multiple Concurrent Executions", async () =>
            {
                var tasks = new List<Task<TestAggregationResult>>();
                
                for (int i = 0; i < 3; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var aggregator = new TestAggregator();
                        aggregator.DiscoverDefaultTests();
                        return await aggregator.RunAllTestsAsync();
                    }));
                }
                
                var results = await Task.WhenAll(tasks);
                
                foreach (var result in results)
                {
                    if (result.TotalTests != 6)
                        throw new Exception($"Concurrent execution: Expected 6 tests, got {result.TotalTests}");
                    
                    if (result.PassedTests != 6)
                        throw new Exception($"Concurrent execution: Expected 6 passed tests, got {result.PassedTests}");
                }
            });

            // Test 5.2: Large Test Collection
            await RunSmokeTest("Large Test Collection", async () =>
            {
                var aggregator = new TestAggregator();
                
                // Add many tests
                for (int i = 0; i < 20; i++)
                {
                    aggregator.AddTest(new TestInfo(
                        $"large-test-{i}", 
                        $"Large Test {i}", 
                        $"Description {i}", 
                        "Unit", 
                        "Medium", 
                        2, 
                        1, 
                        new[] { $"tag{i}" }
                    ));
                }
                
                if (aggregator.TestCount != 20)
                    throw new Exception($"Large collection: Expected 20 tests, got {aggregator.TestCount}");
                
                var result = await aggregator.RunAllTestsAsync();
                
                if (result.TotalTests != 20)
                    throw new Exception($"Large collection execution: Expected 20 tests, got {result.TotalTests}");
                
                if (result.PassedTests != 20)
                    throw new Exception($"Large collection execution: Expected 20 passed tests, got {result.PassedTests}");
            });

            // Test 5.3: Performance Timing
            await RunSmokeTest("Performance Timing", async () =>
            {
                var aggregator = new TestAggregator();
                aggregator.DiscoverDefaultTests();
                
                var stopwatch = Stopwatch.StartNew();
                var result = await aggregator.RunAllTestsAsync();
                stopwatch.Stop();
                
                // Should complete within reasonable time (less than 30 seconds)
                if (stopwatch.Elapsed.TotalSeconds > 30)
                    throw new Exception($"Performance: Execution took too long: {stopwatch.Elapsed.TotalSeconds:F1}s");
                
                // Should have reasonable duration
                if (result.TotalDuration.TotalSeconds < 5)
                    throw new Exception($"Performance: Duration too short: {result.TotalDuration.TotalSeconds:F1}s");
            });

            Console.WriteLine();
        }

        private async Task RunIntegrationTests()
        {
            Console.WriteLine("🧪 Test Suite 6: Integration Tests");
            Console.WriteLine("---------------------------------");

            // Test 6.1: Full Workflow Integration
            await RunSmokeTest("Full Workflow Integration", async () =>
            {
                var aggregator = new TestAggregator(verbose: true);
                
                // Step 1: Discover tests
                aggregator.DiscoverDefaultTests();
                if (aggregator.TestCount != 6)
                    throw new Exception($"Integration: Expected 6 discovered tests, got {aggregator.TestCount}");
                
                // Step 2: Run all tests
                var allResult = await aggregator.RunAllTestsAsync(progress: true);
                if (allResult.TotalTests != 6)
                    throw new Exception($"Integration: Expected 6 total tests, got {allResult.TotalTests}");
                
                // Step 3: Run category filtered tests
                var unitResult = await aggregator.RunTestsByCategoryAsync("Unit");
                if (unitResult.TotalTests != 3)
                    throw new Exception($"Integration: Expected 3 Unit tests, got {unitResult.TotalTests}");
                
                // Step 4: Run priority filtered tests
                var highResult = await aggregator.RunTestsByPriorityAsync("High");
                if (highResult.TotalTests != 3)
                    throw new Exception($"Integration: Expected 3 High priority tests, got {highResult.TotalTests}");
                
                // Step 5: Clear and verify
                aggregator.ClearTests();
                if (aggregator.TestCount != 0)
                    throw new Exception($"Integration: Expected 0 tests after clear, got {aggregator.TestCount}");
            });

            // Test 6.2: Result Properties Integration
            await RunSmokeTest("Result Properties Integration", async () =>
            {
                var aggregator = new TestAggregator();
                aggregator.DiscoverDefaultTests();
                
                var result = await aggregator.RunAllTestsAsync();
                
                // Verify all result properties are accessible and valid
                if (result.TotalTests <= 0)
                    throw new Exception("Integration: TotalTests should be > 0");
                
                if (result.PassedTests < 0)
                    throw new Exception("Integration: PassedTests should be >= 0");
                
                if (result.FailedTests < 0)
                    throw new Exception("Integration: FailedTests should be >= 0");
                
                if (result.SkippedTests < 0)
                    throw new Exception("Integration: SkippedTests should be >= 0");
                
                if (result.TotalDuration.TotalSeconds <= 0)
                    throw new Exception("Integration: TotalDuration should be > 0");
                
                if (result.AverageDuration <= 0)
                    throw new Exception("Integration: AverageDuration should be > 0");
                
                if (result.TestResults == null || result.TestResults.Count != result.TotalTests)
                    throw new Exception("Integration: TestResults should match TotalTests");
                
                if (result.Tests == null || result.Tests.Count != result.TotalTests)
                    throw new Exception("Integration: Tests should match TotalTests");
                
                if (result.Metrics == null)
                    throw new Exception("Integration: Metrics should not be null");
            });

            // Test 6.3: Metrics Integration
            await RunSmokeTest("Metrics Integration", async () =>
            {
                var aggregator = new TestAggregator();
                aggregator.DiscoverDefaultTests();
                
                var result = await aggregator.RunAllTestsAsync();
                var metrics = result.Metrics;
                
                // Verify metrics are calculated correctly
                if (metrics.TotalTests != result.TotalTests)
                    throw new Exception($"Metrics: TotalTests mismatch: {metrics.TotalTests} vs {result.TotalTests}");
                
                if (metrics.PassedTests != result.PassedTests)
                    throw new Exception($"Metrics: PassedTests mismatch: {metrics.PassedTests} vs {result.PassedTests}");
                
                if (metrics.FailedTests != result.FailedTests)
                    throw new Exception($"Metrics: FailedTests mismatch: {metrics.FailedTests} vs {result.FailedTests}");
                
                if (metrics.TestsByCategory == null || metrics.TestsByCategory.Count == 0)
                    throw new Exception("Metrics: TestsByCategory should not be null or empty");
                
                if (metrics.TestsByPriority == null || metrics.TestsByPriority.Count == 0)
                    throw new Exception("Metrics: TestsByPriority should not be null or empty");
                
                // Verify category distribution
                var expectedCategories = new[] { "Unit", "Performance", "Integration", "Security" };
                foreach (var category in expectedCategories)
                {
                    if (!metrics.TestsByCategory.ContainsKey(category))
                        throw new Exception($"Metrics: Missing category {category}");
                }
                
                // Verify priority distribution
                var expectedPriorities = new[] { "High", "Medium", "Critical" };
                foreach (var priority in expectedPriorities)
                {
                    if (!metrics.TestsByPriority.ContainsKey(priority))
                        throw new Exception($"Metrics: Missing priority {priority}");
                }
            });

            Console.WriteLine();
        }

        private async Task RunSmokeTest(string testName, Func<Task> testAction)
        {
            _totalTests++;
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                await testAction();
                _passedTests++;
                stopwatch.Stop();
                
                var result = new SmokeTestResult(testName, true, stopwatch.Elapsed, null);
                _results.Add(result);
                
                Console.WriteLine($"   ✅ {testName} - PASSED ({stopwatch.Elapsed.TotalMilliseconds:F0}ms)");
            }
            catch (Exception ex)
            {
                _failedTests++;
                stopwatch.Stop();
                
                var result = new SmokeTestResult(testName, false, stopwatch.Elapsed, ex.Message);
                _results.Add(result);
                
                Console.WriteLine($"   ❌ {testName} - FAILED ({stopwatch.Elapsed.TotalMilliseconds:F0}ms)");
                Console.WriteLine($"      Error: {ex.Message}");
            }
        }

        private void GenerateSmokeTestReport(TimeSpan totalDuration)
        {
            Console.WriteLine("🔥 Smoke Test Report");
            Console.WriteLine("===================");
            Console.WriteLine($"Total Tests: {_totalTests}");
            Console.WriteLine($"Passed: {_passedTests} ✅");
            Console.WriteLine($"Failed: {_failedTests} ❌");
            Console.WriteLine($"Success Rate: {(_passedTests / (double)_totalTests * 100):F1}%");
            Console.WriteLine($"Total Duration: {totalDuration.TotalSeconds:F1}s");
            Console.WriteLine();

            if (_failedTests > 0)
            {
                Console.WriteLine("❌ Failed Tests:");
                foreach (var result in _results.Where(r => !r.Passed))
                {
                    Console.WriteLine($"   • {result.TestName}: {result.ErrorMessage}");
                }
                Console.WriteLine();
            }

            // Group by test suite
            var testSuites = new Dictionary<string, List<SmokeTestResult>>();
            foreach (var result in _results)
            {
                var suite = result.TestName.Split(':')[0];
                if (!testSuites.ContainsKey(suite))
                    testSuites[suite] = new List<SmokeTestResult>();
                testSuites[suite].Add(result);
            }

            Console.WriteLine("📊 Test Suite Summary:");
            foreach (var suite in testSuites)
            {
                var suitePassed = suite.Value.Count(r => r.Passed);
                var suiteTotal = suite.Value.Count;
                var suiteRate = (suitePassed / (double)suiteTotal * 100);
                
                Console.WriteLine($"   {suite.Key}: {suitePassed}/{suiteTotal} ({suiteRate:F1}%)");
            }
            Console.WriteLine();

            var overallSuccess = _failedTests == 0;
            Console.WriteLine($"🎯 Overall Result: {(overallSuccess ? "ALL SMOKE TESTS PASSED! 🏆" : "SOME SMOKE TESTS FAILED ❌")}");
            
            if (overallSuccess)
            {
                Console.WriteLine("✅ Test Aggregator is ready for production use!");
                Console.WriteLine("✅ All core functionality verified and working correctly!");
                Console.WriteLine("✅ Performance and reliability confirmed!");
            }
            else
            {
                Console.WriteLine("⚠️  Issues detected - review failed tests before production use");
            }
        }
    }

    public class SmokeTestResult
    {
        public string TestName { get; }
        public bool Passed { get; }
        public TimeSpan Duration { get; }
        public string? ErrorMessage { get; }

        public SmokeTestResult(string testName, bool passed, TimeSpan duration, string? errorMessage)
        {
            TestName = testName;
            Passed = passed;
            Duration = duration;
            ErrorMessage = errorMessage;
        }
    }
}
