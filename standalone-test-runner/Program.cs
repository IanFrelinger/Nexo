using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StandaloneTestRunner.Services;

namespace StandaloneTestRunner
{
    /// <summary>
    /// Orchestrator for standalone test runner that delegates to specialized services.
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Testing Standalone Test Runner - No Hanging Tests");
            Console.WriteLine("=============================================");
            Console.WriteLine();

            var testRunner = new TestRunnerService();
            var argumentParser = new ArgumentParserService();
            var testExecutor = new TestExecutorService();
            var testReporter = new TestReporterService();

            try
            {
                // Parse command line arguments
                var options = argumentParser.ParseArguments(args);
                
                if (options.ShowHelp)
                {
                    ShowHelp();
                    return 0;
                }

                // Execute tests based on options
                var result = await testExecutor.ExecuteTestsAsync(options);
                
                // Report results
                testReporter.ReportResults(result);

                return result.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Standalone Test Runner");
            Console.WriteLine("=====================");
            Console.WriteLine();
            Console.WriteLine("Usage: StandaloneTestRunner [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --discover                    Discover available tests");
            Console.WriteLine("  --force-timeout               Force timeout on all tests");
            Console.WriteLine("  --progress                    Show progress during execution");
            Console.WriteLine("  --verbose                     Verbose output");
            Console.WriteLine("  --coverage                    Generate coverage report");
            Console.WriteLine("  --smoke-tests                 Run smoke tests only");
            Console.WriteLine("  --epic5-4-tests               Run Epic 5.4 tests");
            Console.WriteLine("  --epic5-4-category            Run Epic 5.4 tests by category");
            Console.WriteLine("  --epic5-4-priority            Run Epic 5.4 tests by priority");
            Console.WriteLine("  --epic5-4-enhanced            Run enhanced Epic 5.4 tests");
            Console.WriteLine("  --epic5-4-phase               Run Epic 5.4 tests by phase");
            Console.WriteLine("  --feature-factory-domain     Run Feature Factory domain tests");
            Console.WriteLine("  --feature-factory-application Run Feature Factory application tests");
            Console.WriteLine("  --feature-factory-deployment Run Feature Factory deployment tests");
            Console.WriteLine("  --ai-services                 Run AI services tests");
            Console.WriteLine("  --core-domain-entities       Run core domain entities tests");
            Console.WriteLine("  --timeout=<seconds>           Set test timeout (default: 5)");
            Console.WriteLine("  --heartbeat-interval=<seconds> Set heartbeat interval (default: 2)");
            Console.WriteLine("  --process-timeout=<minutes>   Set process timeout (default: 1)");
            Console.WriteLine("  --category=<category>         Filter by category");
            Console.WriteLine("  --priority=<priority>        Filter by priority");
            Console.WriteLine("  --help, -h                    Show this help message");
        }
    }
}
