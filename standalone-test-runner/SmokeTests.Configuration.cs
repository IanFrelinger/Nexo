using System;
using System.Threading.Tasks;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Configuration options test suite
    /// </summary>
    public partial class SmokeTests
    {
        private async Task RunConfigurationTests()
        {
            Console.WriteLine("Testing Test Suite 3: Configuration Options");
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
    }
}
