using System;
using System.Threading.Tasks;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Error handling test suite
    /// </summary>
    public partial class SmokeTests
    {
        private async Task RunErrorHandlingTests()
        {
            Console.WriteLine("Testing Test Suite 4: Error Handling");
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
    }
}
