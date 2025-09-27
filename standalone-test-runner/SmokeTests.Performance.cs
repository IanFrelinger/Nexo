using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Performance & Stress Tests test suite
    /// </summary>
    public partial class SmokeTests
    {
        private async Task RunPerformanceTests()
        {
            Console.WriteLine("Testing Test Suite 5: Performance & Stress Tests");
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
    }
}
