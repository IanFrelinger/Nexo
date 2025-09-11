using System;
using System.Threading.Tasks;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Epic 5.4 Test Runner - Demonstrates Epic 5.4 tests integration with TestAggregator
    /// </summary>
    public class Epic5_4TestRunner
    {
        public static async Task RunEpic5_4TestsAsync()
        {
            Console.WriteLine("🚀 Epic 5.4: Deployment & Integration Test Runner 🚀");
            Console.WriteLine("=====================================================");
            Console.WriteLine();

            var aggregator = new TestAggregator(verbose: true);
            
            // Discover and add Epic 5.4 tests
            var epic5_4TestSuite = new Epic5_4TestSuite(verbose: true);
            var epic5_4Tests = epic5_4TestSuite.DiscoverEpic5_4Tests();
            aggregator.AddTests(epic5_4Tests);

            Console.WriteLine($"📊 Discovered {epic5_4Tests.Count} Epic 5.4 tests");
            Console.WriteLine();

            // Run all Epic 5.4 tests
            Console.WriteLine("🔄 Running Epic 5.4 tests...");
            Console.WriteLine();

            var result = await aggregator.RunAllTestsAsync(progress: true);

            // Display results
            Console.WriteLine();
            Console.WriteLine("📊 Epic 5.4 Test Results Summary:");
            Console.WriteLine("=================================");
            Console.WriteLine($"Total Tests: {result.TotalTests}");
            Console.WriteLine($"Passed: {result.PassedTests} ✅");
            Console.WriteLine($"Failed: {result.FailedTests} ❌");
            Console.WriteLine($"Skipped: {result.SkippedTests} ⏭️");
            Console.WriteLine($"Total Duration: {result.TotalDuration.TotalSeconds:F1}s");
            Console.WriteLine($"Average Duration: {result.AverageDuration:F0}ms per test");
            Console.WriteLine();

            // Display test results by category
            Console.WriteLine("📋 Test Results by Category:");
            Console.WriteLine("=============================");
            foreach (var category in result.Metrics.TestsByCategory)
            {
                var categoryTests = result.TestResults.Where(r => 
                    result.Tests.First(t => t.TestId == r.TestId).Category == category.Key).ToList();
                var passed = categoryTests.Count(r => r.IsSuccess);
                var failed = categoryTests.Count(r => !r.IsSuccess);
                
                Console.WriteLine($"{category.Key}: {passed} passed, {failed} failed ({category.Value} total)");
            }
            Console.WriteLine();

            // Display test results by priority
            Console.WriteLine("🎯 Test Results by Priority:");
            Console.WriteLine("============================");
            foreach (var priority in result.Metrics.TestsByPriority)
            {
                var priorityTests = result.TestResults.Where(r => 
                    result.Tests.First(t => t.TestId == r.TestId).Priority == priority.Key).ToList();
                var passed = priorityTests.Count(r => r.IsSuccess);
                var failed = priorityTests.Count(r => !r.IsSuccess);
                
                Console.WriteLine($"{priority.Key}: {passed} passed, {failed} failed ({priority.Value} total)");
            }
            Console.WriteLine();

            // Display failed tests if any
            if (result.FailedTests > 0)
            {
                Console.WriteLine("❌ Failed Tests:");
                Console.WriteLine("================");
                foreach (var failedTest in result.TestResults.Where(r => !r.IsSuccess))
                {
                    var testInfo = result.Tests.First(t => t.TestId == failedTest.TestId);
                    Console.WriteLine($"• {testInfo.DisplayName} ({testInfo.TestId})");
                    if (!string.IsNullOrEmpty(failedTest.ErrorMessage))
                    {
                        Console.WriteLine($"  Error: {failedTest.ErrorMessage}");
                    }
                }
                Console.WriteLine();
            }

            // Display performance metrics
            Console.WriteLine("⚡ Performance Metrics:");
            Console.WriteLine("=======================");
            Console.WriteLine($"Slow Tests (>1s): {result.Metrics.SlowTests}");
            Console.WriteLine($"Fast Tests (<500ms): {result.Metrics.FastTests}");
            Console.WriteLine($"Total Execution Time: {result.TotalExecutionTime.TotalSeconds:F1}s");
            Console.WriteLine();

            // Final status
            if (result.FailedTests == 0)
            {
                Console.WriteLine("🎉 All Epic 5.4 tests passed successfully! 🎉");
            }
            else
            {
                Console.WriteLine($"⚠️  {result.FailedTests} Epic 5.4 tests failed. Please review the errors above.");
            }

            Console.WriteLine();
            Console.WriteLine("Epic 5.4 test run completed.");
        }

        public static async Task RunEpic5_4TestsByCategoryAsync(string category)
        {
            Console.WriteLine($"🚀 Epic 5.4: {category} Tests 🚀");
            Console.WriteLine("================================");
            Console.WriteLine();

            var aggregator = new TestAggregator(verbose: true);
            aggregator.DiscoverDefaultTests();

            Console.WriteLine($"🔄 Running Epic 5.4 {category} tests...");
            Console.WriteLine();

            var result = await aggregator.RunTestsByCategoryAsync(category, progress: true);

            Console.WriteLine();
            Console.WriteLine($"📊 Epic 5.4 {category} Test Results:");
            Console.WriteLine($"Total: {result.TotalTests}, Passed: {result.PassedTests}, Failed: {result.FailedTests}");
            Console.WriteLine($"Duration: {result.TotalDuration.TotalSeconds:F1}s");
        }

        public static async Task RunEpic5_4TestsByPriorityAsync(string priority)
        {
            Console.WriteLine($"🚀 Epic 5.4: {priority} Priority Tests 🚀");
            Console.WriteLine("=========================================");
            Console.WriteLine();

            var aggregator = new TestAggregator(verbose: true);
            aggregator.DiscoverDefaultTests();

            Console.WriteLine($"🔄 Running Epic 5.4 {priority} priority tests...");
            Console.WriteLine();

            var result = await aggregator.RunTestsByPriorityAsync(priority, progress: true);

            Console.WriteLine();
            Console.WriteLine($"📊 Epic 5.4 {priority} Priority Test Results:");
            Console.WriteLine($"Total: {result.TotalTests}, Passed: {result.PassedTests}, Failed: {result.FailedTests}");
            Console.WriteLine($"Duration: {result.TotalDuration.TotalSeconds:F1}s");
        }
    }
}
