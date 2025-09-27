using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Basic functionality test suite
    /// </summary>
    public partial class SmokeTests
    {
        private async Task RunBasicFunctionalityTests()
        {
            Console.WriteLine("Testing Test Suite 1: Basic Functionality");
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
    }
}
