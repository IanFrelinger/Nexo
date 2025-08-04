using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Logging.Tests.Commands;
using System;

namespace Nexo.Feature.Logging.Tests
{
    /// <summary>
    /// Pipeline-architecture test suite for Nexo.Feature.Logging layer.
    /// Uses command classes with proper timeouts and logging to prevent hanging tests.
    /// </summary>
    public class LoggingPipelineTests
    {
        private readonly ILogger<LoggingPipelineTests> _logger;

        public LoggingPipelineTests()
        {
            _logger = NullLogger<LoggingPipelineTests>.Instance;
        }

        [Fact(Timeout = 10000)]
        public void ILogger_Interface_ShouldHaveExpectedMethods()
        {
            _logger.LogInformation("Starting ILogger interface test");
            
            var command = new InterfaceValidationCommand(NullLogger<InterfaceValidationCommand>.Instance);
            var result = command.ValidateILoggerInterface(timeoutMs: 5000);
            
            Assert.True(result, "ILogger interface should have expected methods");
            _logger.LogInformation("ILogger interface test completed successfully");
        }

        [Fact(Timeout = 10000)]
        public void ILoggerGeneric_Interface_ShouldInheritFromILogger()
        {
            _logger.LogInformation("Starting ILogger<T> interface test");
            
            var command = new InterfaceValidationCommand(NullLogger<InterfaceValidationCommand>.Instance);
            var result = command.ValidateILoggerGenericInterface(timeoutMs: 5000);
            
            Assert.True(result, "ILogger<T> interface should inherit from ILogger");
            _logger.LogInformation("ILogger<T> interface test completed successfully");
        }

        [Fact(Timeout = 10000)]
        public void ILoggerFactory_Interface_ShouldHaveExpectedMethods()
        {
            _logger.LogInformation("Starting ILoggerFactory interface test");
            
            var command = new InterfaceValidationCommand(NullLogger<InterfaceValidationCommand>.Instance);
            var result = command.ValidateILoggerFactoryInterface(timeoutMs: 5000);
            
            Assert.True(result, "ILoggerFactory interface should have expected methods");
            _logger.LogInformation("ILoggerFactory interface test completed successfully");
        }

        [Fact(Timeout = 10000)]
        public void ILoggerProvider_Interface_ShouldHaveExpectedMethods()
        {
            _logger.LogInformation("Starting ILoggerProvider interface test");
            
            var command = new InterfaceValidationCommand(NullLogger<InterfaceValidationCommand>.Instance);
            var result = command.ValidateILoggerProviderInterface(timeoutMs: 5000);
            
            Assert.True(result, "ILoggerProvider interface should have expected methods");
            _logger.LogInformation("ILoggerProvider interface test completed successfully");
        }

        [Fact(Timeout = 10000)]
        public void LoggerService_ShouldInstantiateCorrectly()
        {
            _logger.LogInformation("Starting logger service instantiation test");
            
            var command = new ServiceValidationCommand(NullLogger<ServiceValidationCommand>.Instance);
            var result = command.ValidateLoggerServiceInstantiation(timeoutMs: 5000);
            
            Assert.True(result, "Logger service should instantiate correctly");
            _logger.LogInformation("Logger service instantiation test completed successfully");
        }

        [Fact(Timeout = 10000)]
        public void LoggerFactory_ShouldCreateLoggersCorrectly()
        {
            _logger.LogInformation("Starting logger factory functionality test");
            
            var command = new ServiceValidationCommand(NullLogger<ServiceValidationCommand>.Instance);
            var result = command.ValidateLoggerFactoryFunctionality(timeoutMs: 5000);
            
            Assert.True(result, "Logger factory should create loggers correctly");
            _logger.LogInformation("Logger factory functionality test completed successfully");
        }

        [Fact(Timeout = 10000)]
        public void LoggerProvider_ShouldCreateLoggersCorrectly()
        {
            _logger.LogInformation("Starting logger provider functionality test");
            
            var command = new ServiceValidationCommand(NullLogger<ServiceValidationCommand>.Instance);
            var result = command.ValidateLoggerProviderFunctionality(timeoutMs: 5000);
            
            Assert.True(result, "Logger provider should create loggers correctly");
            _logger.LogInformation("Logger provider functionality test completed successfully");
        }

        [Fact(Timeout = 10000)]
        public void LoggerScope_ShouldWorkCorrectly()
        {
            _logger.LogInformation("Starting logger scope functionality test");
            
            var command = new ServiceValidationCommand(NullLogger<ServiceValidationCommand>.Instance);
            var result = command.ValidateLoggerScopeFunctionality(timeoutMs: 5000);
            
            Assert.True(result, "Logger scope should work correctly");
            _logger.LogInformation("Logger scope functionality test completed successfully");
        }
    }
} 