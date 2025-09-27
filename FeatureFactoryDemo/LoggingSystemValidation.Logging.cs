using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FeatureFactoryDemo
{
    /// <summary>
    /// Logging functionality tests
    /// </summary>
    public partial class LoggingSystemValidation
    {
        private async Task<TestResult> TestLogLevelsAsync()
        {
            _logger.LogInformation("Search Testing log levels...");
            
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
            _logger.LogInformation("Search Testing structured logging...");
            
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
            _logger.LogInformation("Search Testing exception logging...");
            
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
            _logger.LogInformation("Search Testing scope functionality...");
            
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
    }
}
