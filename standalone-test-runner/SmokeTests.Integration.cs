using System;
using System.Linq;
using System.Threading.Tasks;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Integration Tests test suite
    /// </summary>
    public partial class SmokeTests
    {
        private async Task RunIntegrationTests()
        {
            Console.WriteLine("Testing Test Suite 6: Integration Tests");
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
    }
}
