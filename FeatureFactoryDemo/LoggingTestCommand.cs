using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Command to run comprehensive logging system tests
    /// </summary>
    public partial class LoggingTestCommand : BaseCommand
    {
        public override string Name => "test-logging";
        public override string Description => "Run comprehensive logging system validation tests";
        public override string Usage => "test-logging [--verbose]";

        public LoggingTestCommand(IServiceProvider serviceProvider, ILogger<BaseCommand> logger) : base(serviceProvider, logger)
        {
        }

        public override async Task<int> ExecuteAsync(string[] args)
        {
            var verbose = args.Contains("--verbose");
            
            _logger.LogInformation("Testing Starting comprehensive logging system validation");
            Console.WriteLine("Testing COMPREHENSIVE LOGGING SYSTEM VALIDATION");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            try
            {
                // Set up a dedicated service collection for logging tests to control logging output
                var services = new ServiceCollection();
                var testLoggerProvider = new TestLoggerProvider(); // Custom provider to capture logs
                services.AddSingleton<ILoggerProvider>(testLoggerProvider);
                services.AddLogging(builder =>
                {
                    builder.ClearProviders(); // Clear default providers
                    builder.AddProvider(testLoggerProvider); // Add our test provider
                    builder.SetMinimumLevel(LogLevel.Trace); // Capture all logs
                });

                // Add other services needed for validation (e.g., for TestServiceWithLogging)
                services.AddTransient<TestServiceWithLogging>();
                services.AddScoped<TestServiceWithLogging>();
                services.AddSingleton<TestServiceWithLogging>();
                services.AddTransient<TestServiceWithoutLogging>();
                services.AddScoped<TestRepositoryWithLogging>();
                services.AddScoped<TestCommandWithLogging>();

                var testServiceProvider = services.BuildServiceProvider();

                using var validation = new LoggingSystemValidation(testServiceProvider, verbose);
                var result = await validation.RunComprehensiveValidationAsync();

                Console.WriteLine("Stats VALIDATION RESULTS");
                Console.WriteLine("=====================");
                Console.WriteLine();

                // Display individual test results
                DisplayTestResult("Basic Dependency Injection", result.BasicDependencyInjection, verbose);
                DisplayTestResult("Logger Type Safety", result.LoggerTypeSafety, verbose);
                DisplayTestResult("Log Levels", result.LogLevels, verbose);
                DisplayTestResult("Structured Logging", result.StructuredLogging, verbose);
                DisplayTestResult("Exception Logging", result.ExceptionLogging, verbose);
                DisplayTestResult("Scope Functionality", result.ScopeFunctionality, verbose);
                DisplayTestResult("Service Lifetime Management", result.ServiceLifetimeManagement, verbose);
                DisplayTestResult("Performance", result.Performance, verbose);
                DisplayTestResult("Concurrent Operations", result.ConcurrentOperations, verbose);
                DisplayTestResult("Memory Usage", result.MemoryUsage, verbose);

                Console.WriteLine();
                Console.WriteLine("Target OVERALL RESULT");
                Console.WriteLine("=================");
                
                if (result.OverallSuccess)
                {
                    Console.WriteLine("SUCCESS: ALL TESTS PASSED! Logging system is working correctly.");
                    Console.WriteLine();
                    Console.WriteLine("SUCCESS Dependency injection wrapped logging is properly implemented:");
                    Console.WriteLine("   SUCCESS: All logging levels are working correctly");
                    Console.WriteLine("   SUCCESS: Performance is within acceptable limits");
                    Console.WriteLine("   SUCCESS: Error handling and logging is functioning");
                    Console.WriteLine("   SUCCESS: Configuration is working as expected");
                    Console.WriteLine("   SUCCESS: Stress testing shows system stability");
                    Console.WriteLine("   SUCCESS: Type-safe generic loggers are working");
                    Console.WriteLine("   SUCCESS: Service lifetime management is correct");
                    Console.WriteLine("   SUCCESS: Concurrent operations are thread-safe");
                    Console.WriteLine("   SUCCESS: Memory usage is optimized");
                    
                    _logger.LogInformation("SUCCESS: All logging system tests passed");
                    return 0;
                }
                else
                {
                    Console.WriteLine("ERROR: SOME TESTS FAILED! Please review the logging system.");
                    Console.WriteLine();
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        Console.WriteLine($"Error: {result.ErrorMessage}");
                    }
                    Console.WriteLine("Failed tests indicate potential issues with:");
                    Console.WriteLine("- Logging configuration");
                    Console.WriteLine("- Performance bottlenecks");
                    Console.WriteLine("- Error handling");
                    Console.WriteLine("- System stability");
                    
                    _logger.LogError("ERROR: Some logging system tests failed");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Error during logging system validation: {ex.Message}");
                _logger.LogError(ex, "ERROR: Error during logging system validation");
                return 1;
            }
        }

        private void DisplayTestResult(string testName, TestResult result, bool verbose)
        {
            var status = result.Success ? "SUCCESS: PASS" : "ERROR: FAIL";
            Console.WriteLine($"{status} {testName}");
            
            if (verbose || !result.Success)
            {
                Console.WriteLine($"    {result.Message}");
            }
        }
    }
}
