using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FeatureFactoryDemo
{
    /// <summary>
    /// Core logging system validation functionality
    /// </summary>
    public partial class LoggingSystemValidation : IDisposable
    {
        private readonly ILogger<LoggingSystemValidation> _logger;
        private readonly ServiceProvider _serviceProvider;
        private readonly TestLoggerProvider _testLoggerProvider;

        public LoggingSystemValidation()
        {
            // Set up dependency injection with test logging
            var services = new ServiceCollection();
            
            // Add test logging provider
            _testLoggerProvider = new TestLoggerProvider();
            services.AddLogging(builder =>
            {
                builder.AddProvider(_testLoggerProvider);
                // Don't add console logging to avoid flooding output
                builder.SetMinimumLevel(LogLevel.Trace);
            });
            
            // Add test services
            services.AddScoped<TestServiceWithLogging>();
            services.AddScoped<TestRepositoryWithLogging>();
            services.AddScoped<TestCommandWithLogging>();
            
            _serviceProvider = services.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<LoggingSystemValidation>>();
        }

        public LoggingSystemValidation(ServiceProvider serviceProvider, bool verbose = false)
        {
            _serviceProvider = serviceProvider;
            _testLoggerProvider = serviceProvider.GetRequiredService<ILoggerProvider>() as TestLoggerProvider ?? new TestLoggerProvider();
            _logger = serviceProvider.GetRequiredService<ILogger<LoggingSystemValidation>>();
        }

        /// <summary>
        /// Run comprehensive logging system validation
        /// </summary>
        public async Task<ValidationResult> RunComprehensiveValidationAsync()
        {
            var result = new ValidationResult();
            _logger.LogInformation("Testing Starting comprehensive logging system validation");

            try
            {
                // Test 1: Basic Dependency Injection
                result.BasicDependencyInjection = await TestBasicDependencyInjectionAsync();
                
                // Test 2: Logger Type Safety
                result.LoggerTypeSafety = await TestLoggerTypeSafetyAsync();
                
                // Test 3: Log Levels
                result.LogLevels = await TestLogLevelsAsync();
                
                // Test 4: Structured Logging
                result.StructuredLogging = await TestStructuredLoggingAsync();
                
                // Test 5: Exception Logging
                result.ExceptionLogging = await TestExceptionLoggingAsync();
                
                // Test 6: Scope Functionality
                result.ScopeFunctionality = await TestScopeFunctionalityAsync();
                
                // Test 7: Service Lifetime Management
                result.ServiceLifetimeManagement = await TestServiceLifetimeManagementAsync();
                
                // Test 8: Performance
                result.Performance = await TestPerformanceAsync();
                
                // Test 9: Concurrent Operations
                result.ConcurrentOperations = await TestConcurrentOperationsAsync();
                
                // Test 10: Memory Usage
                result.MemoryUsage = await TestMemoryUsageAsync();

                result.OverallSuccess = result.BasicDependencyInjection.Success &&
                                      result.LoggerTypeSafety.Success &&
                                      result.LogLevels.Success &&
                                      result.StructuredLogging.Success &&
                                      result.ExceptionLogging.Success &&
                                      result.ScopeFunctionality.Success &&
                                      result.ServiceLifetimeManagement.Success &&
                                      result.Performance.Success &&
                                      result.ConcurrentOperations.Success &&
                                      result.MemoryUsage.Success;

                _logger.LogInformation("SUCCESS Comprehensive logging system validation completed: {Success}", 
                    result.OverallSuccess ? "PASSED" : "FAILED");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR: Error during logging system validation");
                result.OverallSuccess = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
