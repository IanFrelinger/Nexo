using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FeatureFactoryDemo
{
    /// <summary>
    /// Comprehensive logging system validation for dependency injection wrapped logging
    /// </summary>
    public class LoggingSystemValidation : IDisposable
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
            _logger = _serviceProvider.GetRequiredService<ILogger<LoggingSystemValidation>>();
        }

        /// <summary>
        /// Run comprehensive logging system validation
        /// </summary>
        public async Task<ValidationResult> RunComprehensiveValidationAsync()
        {
            var result = new ValidationResult();
            _logger.LogInformation("🧪 Starting comprehensive logging system validation");

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

                _logger.LogInformation("🎉 Comprehensive logging system validation completed: {Success}", 
                    result.OverallSuccess ? "PASSED" : "FAILED");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during logging system validation");
                result.OverallSuccess = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<TestResult> TestBasicDependencyInjectionAsync()
        {
            _logger.LogInformation("🔍 Testing basic dependency injection...");
            
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
            _logger.LogInformation("🔍 Testing logger type safety...");
            
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

        private async Task<TestResult> TestLogLevelsAsync()
        {
            _logger.LogInformation("🔍 Testing log levels...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                _testLoggerProvider.ClearLogs();
                
                testService.LogAllLevels();
                
                var logs = _testLoggerProvider.GetLogs();
                var expectedLevels = new[] { LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical };
                
                foreach (var level in expectedLevels)
                {
                    if (!logs.Any(l => l.Level == level))
                    {
                        return new TestResult { Success = false, Message = $"Log level {level} not found" };
                    }
                }

                return new TestResult { Success = true, Message = "All log levels working correctly" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Log levels test failed: {ex.Message}" };
            }
        }

        private async Task<TestResult> TestStructuredLoggingAsync()
        {
            _logger.LogInformation("🔍 Testing structured logging...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                _testLoggerProvider.ClearLogs();
                
                testService.LogStructuredMessage("TestUser", 42, true);
                
                var logs = _testLoggerProvider.GetLogs();
                var structuredLog = logs.FirstOrDefault(l => l.Message.Contains("User operation"));
                
                if (structuredLog == null)
                {
                    return new TestResult { Success = false, Message = "Structured log not found" };
                }

                return new TestResult { Success = true, Message = "Structured logging working correctly" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Structured logging test failed: {ex.Message}" };
            }
        }

        private async Task<TestResult> TestExceptionLoggingAsync()
        {
            _logger.LogInformation("🔍 Testing exception logging...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                _testLoggerProvider.ClearLogs();
                var testException = new InvalidOperationException("Test exception");
                
                testService.LogException(testException);
                
                var logs = _testLoggerProvider.GetLogs();
                var exceptionLog = logs.FirstOrDefault(l => l.Exception != null);
                
                if (exceptionLog == null || exceptionLog.Exception != testException)
                {
                    return new TestResult { Success = false, Message = "Exception logging failed" };
                }

                return new TestResult { Success = true, Message = "Exception logging working correctly" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Exception logging test failed: {ex.Message}" };
            }
        }

        private async Task<TestResult> TestScopeFunctionalityAsync()
        {
            _logger.LogInformation("🔍 Testing scope functionality...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                _testLoggerProvider.ClearLogs();
                
                testService.UseScope();
                
                var logs = _testLoggerProvider.GetLogs();
                if (!logs.Any(l => l.Message.Contains("Inside scope")) || !logs.Any(l => l.Message.Contains("Outside scope")))
                {
                    return new TestResult { Success = false, Message = "Scope functionality failed" };
                }

                return new TestResult { Success = true, Message = "Scope functionality working correctly" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Scope functionality test failed: {ex.Message}" };
            }
        }

        private async Task<TestResult> TestServiceLifetimeManagementAsync()
        {
            _logger.LogInformation("🔍 Testing service lifetime management...");
            
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

        private async Task<TestResult> TestPerformanceAsync()
        {
            _logger.LogInformation("🔍 Testing logging performance...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                var iterations = 1000;
                var stopwatch = Stopwatch.StartNew();
                
                for (int i = 0; i < iterations; i++)
                {
                    testService.Logger.LogInformation("Performance test message {Iteration}", i);
                }
                stopwatch.Stop();
                
                var averageTime = stopwatch.ElapsedMilliseconds / (double)iterations;
                var logsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
                
                if (averageTime > 0.1 || logsPerSecond < 1000)
                {
                    return new TestResult { Success = false, Message = $"Performance too slow: {averageTime:F4}ms per log, {logsPerSecond:F0} logs/sec" };
                }

                return new TestResult { Success = true, Message = $"Performance acceptable: {averageTime:F4}ms per log, {logsPerSecond:F0} logs/sec" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Performance test failed: {ex.Message}" };
            }
        }

        private async Task<TestResult> TestConcurrentOperationsAsync()
        {
            _logger.LogInformation("🔍 Testing concurrent operations...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                var iterations = 100;
                var concurrentTasks = 10;
                var stopwatch = Stopwatch.StartNew();
                
                var tasks = new List<Task>();
                for (int task = 0; task < concurrentTasks; task++)
                {
                    int taskId = task;
                    tasks.Add(Task.Run(() =>
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            testService.Logger.LogInformation("Concurrent test message {TaskId} {Iteration}", taskId, i);
                        }
                    }));
                }
                
                await Task.WhenAll(tasks);
                stopwatch.Stop();
                
                var totalLogs = iterations * concurrentTasks;
                var logsPerSecond = totalLogs / stopwatch.Elapsed.TotalSeconds;
                
                if (logsPerSecond < 100)
                {
                    return new TestResult { Success = false, Message = $"Concurrent performance too slow: {logsPerSecond:F0} logs/sec" };
                }

                return new TestResult { Success = true, Message = $"Concurrent operations working: {logsPerSecond:F0} logs/sec" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Concurrent operations test failed: {ex.Message}" };
            }
        }

        private async Task<TestResult> TestMemoryUsageAsync()
        {
            _logger.LogInformation("🔍 Testing memory usage...");
            
            try
            {
                var testService = _serviceProvider.GetRequiredService<TestServiceWithLogging>();
                var iterations = 1000;
                var initialMemory = GC.GetTotalMemory(true);
                
                for (int i = 0; i < iterations; i++)
                {
                    testService.Logger.LogInformation("Memory test message {Iteration} with additional data", i);
                }
                
                var finalMemory = GC.GetTotalMemory(false);
                var memoryIncrease = finalMemory - initialMemory;
                var averageMemoryPerLog = memoryIncrease / (double)iterations;
                
                if (averageMemoryPerLog > 1000)
                {
                    return new TestResult { Success = false, Message = $"Memory usage too high: {averageMemoryPerLog:F2} bytes per log" };
                }

                return new TestResult { Success = true, Message = $"Memory usage acceptable: {averageMemoryPerLog:F2} bytes per log" };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Memory usage test failed: {ex.Message}" };
            }
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// Test service that uses logging to demonstrate DI patterns
    /// </summary>
    public class TestServiceWithLogging
    {
        public ILogger<TestServiceWithLogging> Logger { get; }

        public TestServiceWithLogging(ILogger<TestServiceWithLogging> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogAllLevels()
        {
            Logger.LogTrace("Trace message");
            Logger.LogDebug("Debug message");
            Logger.LogInformation("Information message");
            Logger.LogWarning("Warning message");
            Logger.LogError("Error message");
            Logger.LogCritical("Critical message");
        }

        public void LogStructuredMessage(string userName, int userId, bool isActive)
        {
            Logger.LogInformation("User operation completed for {UserName} (ID: {UserId}, Active: {IsActive})", 
                userName, userId, isActive);
        }

        public void LogException(Exception exception)
        {
            Logger.LogError(exception, "An error occurred during operation");
        }

        public void UseScope()
        {
            Logger.LogInformation("Outside scope");
            using (Logger.BeginScope("OperationScope"))
            {
                Logger.LogInformation("Inside scope");
            }
            Logger.LogInformation("Outside scope again");
        }

        public async Task LogAsyncOperation()
        {
            Logger.LogInformation("Async operation started");
            await Task.Delay(10);
            Logger.LogInformation("Async operation completed");
        }
    }

    /// <summary>
    /// Test repository that uses logging to demonstrate DI patterns
    /// </summary>
    public class TestRepositoryWithLogging
    {
        public ILogger<TestRepositoryWithLogging> Logger { get; }

        public TestRepositoryWithLogging(ILogger<TestRepositoryWithLogging> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogRepositoryOperation()
        {
            Logger.LogInformation("Repository operation executed");
        }
    }

    /// <summary>
    /// Test command that uses logging to demonstrate DI patterns
    /// </summary>
    public class TestCommandWithLogging
    {
        public ILogger<TestCommandWithLogging> Logger { get; }

        public TestCommandWithLogging(ILogger<TestCommandWithLogging> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogCommandExecution()
        {
            Logger.LogInformation("Command executed successfully");
        }
    }

    /// <summary>
    /// Test service without logging to demonstrate DI patterns
    /// </summary>
    public class TestServiceWithoutLogging
    {
        public void DoWork()
        {
            // This service doesn't use logging
        }
    }

    /// <summary>
    /// Test logger provider that captures logs for testing
    /// </summary>
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly List<TestLogEntry> _logs = new();
        private readonly object _lock = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, this);
        }

        public void AddLog(TestLogEntry logEntry)
        {
            lock (_lock)
            {
                _logs.Add(logEntry);
            }
        }

        public List<TestLogEntry> GetLogs()
        {
            lock (_lock)
            {
                return new List<TestLogEntry>(_logs);
            }
        }

        public void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// Test logger implementation
    /// </summary>
    public class TestLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly TestLoggerProvider _provider;

        public TestLogger(string categoryName, TestLoggerProvider provider)
        {
            _categoryName = categoryName;
            _provider = provider;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new TestScope();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true; // Enable all levels for testing
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var logEntry = new TestLogEntry
            {
                Level = logLevel,
                Message = message,
                Exception = exception,
                CategoryName = _categoryName,
                EventId = eventId,
                Timestamp = DateTime.UtcNow
            };
            _provider.AddLog(logEntry);
        }
    }

    /// <summary>
    /// Test log entry model
    /// </summary>
    public class TestLogEntry
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public EventId EventId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Test scope implementation
    /// </summary>
    public class TestScope : IDisposable
    {
        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// Validation result model
    /// </summary>
    public class ValidationResult
    {
        public bool OverallSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public TestResult BasicDependencyInjection { get; set; } = new();
        public TestResult LoggerTypeSafety { get; set; } = new();
        public TestResult LogLevels { get; set; } = new();
        public TestResult StructuredLogging { get; set; } = new();
        public TestResult ExceptionLogging { get; set; } = new();
        public TestResult ScopeFunctionality { get; set; } = new();
        public TestResult ServiceLifetimeManagement { get; set; } = new();
        public TestResult Performance { get; set; } = new();
        public TestResult ConcurrentOperations { get; set; } = new();
        public TestResult MemoryUsage { get; set; } = new();
    }

    /// <summary>
    /// Test result model
    /// </summary>
    public class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
