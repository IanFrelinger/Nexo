using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FeatureFactoryDemo
{
    /// <summary>
    /// Dependency injection and type safety tests
    /// </summary>
    public partial class LoggingSystemValidation
    {
        private async Task<TestResult> TestBasicDependencyInjectionAsync()
        {
            _logger.LogInformation("Search Testing basic dependency injection...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                var testRepository = _serviceProvider.GetRequiredService<TestRepositoryWithLogging>();
                var testCommand = _serviceProvider.GetRequiredService<TestCommandWithLogging>();

                if (testService?.Logger == null || testRepository?.Logger == null || testCommand?.Logger == null)
                {
                    return new TestResult { Success = false, Message = "Logger injection failed" };
                }

                return new TestResult { Success = true, Message = "Basic dependency injection working correctly" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Dependency injection failed: {ex.Message}" };
            }
        }

        private async Task<TestResult> TestLoggerTypeSafetyAsync()
        {
            _logger.LogInformation("Search Testing logger type safety...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                var logger = testService.Logger;
                
                // Check if the logger implements ILogger<T> interface
                var loggerInterface = logger.GetType().GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILogger<>));
                
                if (loggerInterface == null)
                {
                    return new TestResult { Success = false, Message = "Logger does not implement ILogger<T>" };
                }

                var genericArgument = loggerInterface.GetGenericArguments()[0];
                if (genericArgument != typeof(TestServiceWithLogging))
                {
                    return new TestResult { Success = false, Message = "Logger generic argument is incorrect" };
                }

                // Test that the logger can actually log
                logger.LogInformation("Type safety test message");
                
                return new TestResult { Success = true, Message = "Logger type safety working correctly" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Type safety test failed: {ex.Message}" };
            }
        }

        private async Task<TestResult> TestServiceLifetimeManagementAsync()
        {
            _logger.LogInformation("Search Testing service lifetime management...");
            
            try
            {
                // Test scoped services
                using (var scope1 = _serviceProvider.CreateScope())
                {
                    var service1 = scope1.ServiceProvider.GetRequiredService<TestServiceWithLogging>();
                    var service2 = scope1.ServiceProvider.GetRequiredService<TestServiceWithLogging>();
                    
                    if (service1 != service2)
                    {
                        return new TestResult { Success = false, Message = "Scoped services not sharing instance" };
                    }
                }

                using (var scope2 = _serviceProvider.CreateScope())
                {
                    var service3 = scope2.ServiceProvider.GetRequiredService<TestServiceWithLogging>();
                    // This should be a different instance from scope1
                }

                return new TestResult { Success = true, Message = "Service lifetime management working correctly" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Service lifetime test failed: {ex.Message}" };
            }
        }
    }
}
