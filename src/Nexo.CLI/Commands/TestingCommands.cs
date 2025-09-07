using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Runner;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.CLI.Commands
{
    public static class TestingCommands
    {
        public static Command CreateTestingCommand(IServiceProvider serviceProvider, ILogger logger)
        {
            var testingCommand = new Command("test", "Testing commands for Feature Factory validation");
            
            var featureFactoryTestCommand = new Command("feature-factory", "Test Feature Factory components using C# test runner");
            var validateE2EOption = new Option<bool>("--validate-e2e", "Run end-to-end validation tests");
            var outputOption = new Option<string>("--output", "Output directory for test results");
            var verboseOption = new Option<bool>("--verbose", "Enable verbose output");
            var timeoutOption = new Option<int>("--timeout", () => 5, "Default timeout in minutes for test commands");
            var aiTimeoutOption = new Option<int>("--ai-timeout", () => 30, "Timeout in seconds for AI connectivity tests");
            var domainTimeoutOption = new Option<int>("--domain-timeout", () => 2, "Timeout in minutes for domain analysis tests");
            var codeTimeoutOption = new Option<int>("--code-timeout", () => 3, "Timeout in minutes for code generation tests");
            var e2eTimeoutOption = new Option<int>("--e2e-timeout", () => 5, "Timeout in minutes for end-to-end tests");
            var perfTimeoutOption = new Option<int>("--perf-timeout", () => 2, "Timeout in minutes for performance tests");
            var filterOption = new Option<string>("--filter", "Filter tests by category, priority, or tags (e.g., 'critical', 'ai', 'performance')");
            var discoverOption = new Option<bool>("--discover", "Discover and list available tests without running them");
            var coverageOption = new Option<bool>("--coverage", "Enable test coverage analysis and reporting");
            var progressOption = new Option<bool>("--progress", "Enable real-time progress reporting");
            var coverageThresholdOption = new Option<double>("--coverage-threshold", () => 80.0, "Minimum coverage percentage threshold");
            var forceTimeoutOption = new Option<bool>("--force-timeout", "Enable force timeout and cancellation for stuck tests");
            var heartbeatIntervalOption = new Option<int>("--heartbeat-interval", () => 30, "Heartbeat monitoring interval in seconds");
            var processTimeoutOption = new Option<int>("--process-timeout", () => 15, "Process timeout in minutes");
            
            featureFactoryTestCommand.AddOption(validateE2EOption);
            featureFactoryTestCommand.AddOption(outputOption);
            featureFactoryTestCommand.AddOption(verboseOption);
            featureFactoryTestCommand.AddOption(timeoutOption);
            featureFactoryTestCommand.AddOption(aiTimeoutOption);
            featureFactoryTestCommand.AddOption(domainTimeoutOption);
            featureFactoryTestCommand.AddOption(codeTimeoutOption);
            featureFactoryTestCommand.AddOption(e2eTimeoutOption);
            featureFactoryTestCommand.AddOption(perfTimeoutOption);
            featureFactoryTestCommand.AddOption(filterOption);
            featureFactoryTestCommand.AddOption(discoverOption);
            featureFactoryTestCommand.AddOption(coverageOption);
            featureFactoryTestCommand.AddOption(progressOption);
            featureFactoryTestCommand.AddOption(coverageThresholdOption);
            featureFactoryTestCommand.AddOption(forceTimeoutOption);
            featureFactoryTestCommand.AddOption(heartbeatIntervalOption);
            featureFactoryTestCommand.AddOption(processTimeoutOption);
            
            // TODO: Fix SetHandler signature - too many parameters
            // featureFactoryTestCommand.SetHandler(async (validateE2E, output, verbose, timeout, aiTimeout, domainTimeout, codeTimeout, e2eTimeout, perfTimeout, filter, discover, coverage, progress, coverageThreshold, forceTimeout, heartbeatInterval, processTimeout) =>
            // {
            //     try
            //     {
            //             await HandleFeatureFactoryTestCommand(serviceProvider, logger, validateE2E, output, verbose, 
            //                 timeout, aiTimeout, domainTimeout, codeTimeout, e2eTimeout, perfTimeout, filter, discover, coverage, progress, coverageThreshold, forceTimeout, heartbeatInterval, processTimeout);
            //         }
            //         catch (Exception ex)
            //         {
            //             logger.LogError(ex, "Error running Feature Factory tests");
            //             Console.WriteLine($"❌ Error: {ex.Message}");
            //             Environment.Exit(1);
            //         }
            //     }, validateE2EOption, outputOption, verboseOption, timeoutOption, aiTimeoutOption, domainTimeoutOption, codeTimeoutOption, e2eTimeoutOption, perfTimeoutOption, filterOption, discoverOption, coverageOption, progressOption, coverageThresholdOption, forceTimeoutOption, heartbeatIntervalOption, processTimeoutOption);
            
            testingCommand.AddCommand(featureFactoryTestCommand);
            return testingCommand;
        }
        
        private static async Task HandleFeatureFactoryTestCommand(
            IServiceProvider serviceProvider, 
            ILogger logger, 
            bool validateE2E, 
            string output, 
            bool verbose,
            int timeout,
            int aiTimeout,
            int domainTimeout,
            int codeTimeout,
            int e2eTimeout,
            int perfTimeout,
            string filter,
            bool discover,
            bool coverage,
            bool progress,
            double coverageThreshold,
            bool forceTimeout,
            int heartbeatInterval,
            int processTimeout)
        {
            Console.WriteLine("🧪 Feature Factory Testing (C# Test Runner)");
            Console.WriteLine("===========================================");
            Console.WriteLine($"Default Timeout: {timeout} minutes");
            Console.WriteLine($"AI Connectivity Timeout: {aiTimeout} seconds");
            Console.WriteLine($"Domain Analysis Timeout: {domainTimeout} minutes");
            Console.WriteLine($"Code Generation Timeout: {codeTimeout} minutes");
            Console.WriteLine($"End-to-End Timeout: {e2eTimeout} minutes");
            Console.WriteLine($"Performance Timeout: {perfTimeout} minutes");
            if (!string.IsNullOrEmpty(filter))
            {
                Console.WriteLine($"Filter: {filter}");
            }
                if (coverage)
                {
                Console.WriteLine($"Coverage Analysis: Enabled (threshold: {coverageThreshold:F1}%)");
            }
            if (progress)
            {
                Console.WriteLine("Progress Reporting: Enabled");
            }
            if (forceTimeout)
            {
                Console.WriteLine($"Force Timeout: Enabled (heartbeat: {heartbeatInterval}s, process: {processTimeout}m)");
            }
            Console.WriteLine();
            
            var outputDir = output ?? "./test-results";
                    Directory.CreateDirectory(outputDir);
            
            var configuration = new TestConfiguration
            {
                OutputDirectory = outputDir,
                EnableDetailedLogging = verbose || progress,
                EnablePerformanceMonitoring = coverage,
                MaxParallelExecutions = 1,
                DefaultTimeout = TimeSpan.FromMinutes(timeout),
                AiConnectivityTimeout = TimeSpan.FromSeconds(aiTimeout),
                DomainAnalysisTimeout = TimeSpan.FromMinutes(domainTimeout),
                CodeGenerationTimeout = TimeSpan.FromMinutes(codeTimeout),
                EndToEndTimeout = TimeSpan.FromMinutes(e2eTimeout),
                PerformanceTimeout = TimeSpan.FromMinutes(perfTimeout),
                CleanupAfterExecution = true
            };
            
            // Configure timeout manager if force timeout is enabled
            if (forceTimeout)
            {
                var timeoutManager = serviceProvider.GetRequiredService<Nexo.Feature.Factory.Testing.Timeout.ITimeoutManager>();
                var timeoutConfig = new Nexo.Feature.Factory.Testing.Timeout.TimeoutConfiguration
                {
                    DefaultTimeout = TimeSpan.FromMinutes(timeout),
                    EscalationTimeout = TimeSpan.FromMinutes(timeout * 2),
                    HeartbeatInterval = TimeSpan.FromSeconds(heartbeatInterval),
                    ProcessTimeout = TimeSpan.FromMinutes(processTimeout),
                    EnableForceCancellation = true
                };
                timeoutManager.UpdateConfiguration(timeoutConfig);
            }

            // Create simple test runner to prevent hanging
            var testRunner = new SimpleTestRunner(
                serviceProvider.GetRequiredService<ILogger<SimpleTestRunner>>(),
                serviceProvider);
            
            if (discover)
            {
                Console.WriteLine("🔍 Discovering available tests...");
                var discoveredTests = await testRunner.DiscoverTestsAsync();
                
                Console.WriteLine($"Found {discoveredTests.Count()} tests:");
                foreach (var test in discoveredTests)
                {
                    Console.WriteLine($"  • {test.DisplayName} ({test.Category}, {test.Priority})");
                    if (!string.IsNullOrEmpty(test.Description))
                    {
                        Console.WriteLine($"    {test.Description}");
                    }
                    Console.WriteLine($"    Timeout: {test.Timeout.TotalSeconds}s, Duration: {test.EstimatedDuration.TotalSeconds}s");
                    if (test.Tags.Any())
                    {
                        Console.WriteLine($"    Tags: {string.Join(", ", test.Tags)}");
                    }
                Console.WriteLine();
                }
                    return;
                }

            // Create test filter if specified
            TestFilter? testFilter = null;
            if (!string.IsNullOrEmpty(filter))
            {
                testFilter = CreateTestFilter(filter);
            }
            
            Console.WriteLine("🚀 Executing tests with C# test runner...");
            SimpleTestSummary summary;
            
            // SimpleTestRunner doesn't have RunFilteredTestsAsync, so we'll use RunAllTestsAsync for now
            summary = await testRunner.RunAllTestsAsync(configuration, CancellationToken.None);
            
            Console.WriteLine($"Success Rate: {summary.PassedTests}/{summary.TotalTests} tests passed");
            Console.WriteLine($"Overall Status: {(summary.FailedTests == 0 ? "✅ PASSED" : "❌ FAILED")}");
            
            // Show error information if any tests failed
            if (summary.FailedTests > 0)
            {
                Console.WriteLine();
                Console.WriteLine("❌ Failed tests:");
                foreach (var error in summary.ErrorMessages)
                {
                    Console.WriteLine($"   • {error}");
                }
            }
            
            // Save test results
            var resultsFile = Path.Combine(outputDir, $"test-results-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(resultsFile, json);
            Console.WriteLine($"Test results saved to: {resultsFile}");
            
            Environment.Exit(summary.FailedTests == 0 ? 0 : 1);
        }
        
        private static TestFilter CreateTestFilter(string filter)
        {
            var testFilter = new TestFilter();
            var filterParts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim().ToLowerInvariant())
                .ToList();

            foreach (var part in filterParts)
            {
                switch (part)
                {
                    case "critical":
                        testFilter.IncludePriorities = new[] { TestPriority.Critical };
                        break;
                    case "high":
                        testFilter.IncludePriorities = new[] { TestPriority.High };
                        break;
                    case "medium":
                        testFilter.IncludePriorities = new[] { TestPriority.Medium };
                        break;
                    case "low":
                        testFilter.IncludePriorities = new[] { TestPriority.Low };
                        break;
                    case "ai":
                        testFilter.IncludeTags = new[] { "ai", "connectivity" };
                        break;
                    case "performance":
                        testFilter.IncludeTags = new[] { "performance", "metrics" };
                        break;
                    case "e2e":
                        testFilter.IncludeTags = new[] { "e2e", "integration" };
                        break;
                    case "validation":
                        testFilter.IncludeTags = new[] { "validation", "error-handling" };
                        break;
                    case "unit":
                        testFilter.IncludeCategories = new[] { TestCategory.Unit };
                        break;
                    case "integration":
                        testFilter.IncludeCategories = new[] { TestCategory.Integration };
                        break;
                    case "functional":
                        testFilter.IncludeCategories = new[] { TestCategory.Functional };
                        break;
                    default:
                        // Treat as tag
                        testFilter.IncludeTags = new[] { part };
                        break;
                }
            }

            return testFilter;
        }
    }
}
