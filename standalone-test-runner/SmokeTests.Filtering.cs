using System;
using System.Threading.Tasks;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Filtering capabilities test suite
    /// </summary>
    public partial class SmokeTests
    {
        private async Task RunFilteringTests()
        {
            Console.WriteLine("Testing Test Suite 2: Filtering Capabilities");
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
    }
}
