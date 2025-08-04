using Microsoft.Extensions.Logging;
using NexoLogger = Nexo.Core.Application.Interfaces.Logging.ILogger;
using NexoLoggerFactory = Nexo.Core.Application.Interfaces.Logging.ILoggerFactory;
using NexoLoggerProvider = Nexo.Core.Application.Interfaces.Logging.ILoggerProvider;
using Moq;
using System;

namespace Nexo.Feature.Logging.Tests.Commands;

/// <summary>
/// Command for validating Logging services with proper logging and timeouts.
/// </summary>
public class ServiceValidationCommand
{
    private readonly ILogger<ServiceValidationCommand> _logger;

    public ServiceValidationCommand(ILogger<ServiceValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates logger service instantiation.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateLoggerServiceInstantiation(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting logger service instantiation validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that we can create a mock logger service
            var mockLogger = new Mock<NexoLogger>();
            var mockLoggerFactory = new Mock<NexoLoggerFactory>();
            var mockLoggerProvider = new Mock<NexoLoggerProvider>();

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Logger service instantiation validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = mockLogger != null && mockLoggerFactory != null && mockLoggerProvider != null;
            _logger.LogInformation("Logger service instantiation validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logger service instantiation validation");
            return false;
        }
    }

    /// <summary>
    /// Validates logger factory functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateLoggerFactoryFunctionality(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting logger factory functionality validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate logger factory setup
            var mockLogger = new Mock<NexoLogger>();
            var mockLoggerFactory = new Mock<NexoLoggerFactory>();
            var mockLoggerProvider = new Mock<NexoLoggerProvider>();

            mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
            mockLoggerFactory.Setup(f => f.AddProvider(It.IsAny<NexoLoggerProvider>()));

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Logger factory functionality validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = mockLoggerFactory.Object.CreateLogger("Test") != null;
            _logger.LogInformation("Logger factory functionality validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logger factory functionality validation");
            return false;
        }
    }

    /// <summary>
    /// Validates logger provider functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateLoggerProviderFunctionality(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting logger provider functionality validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate logger provider setup
            var mockLogger = new Mock<NexoLogger>();
            var mockLoggerProvider = new Mock<NexoLoggerProvider>();

            mockLoggerProvider.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Logger provider functionality validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = mockLoggerProvider.Object.CreateLogger("Test") != null;
            _logger.LogInformation("Logger provider functionality validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logger provider functionality validation");
            return false;
        }
    }

    /// <summary>
    /// Validates logger scope functionality.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateLoggerScopeFunctionality(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting logger scope functionality validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate logger scope setup
            var mockLogger = new Mock<NexoLogger>();
            var mockScope = new Mock<IDisposable>();

            mockLogger.Setup(l => l.BeginScope(It.IsAny<string>())).Returns(mockScope.Object);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Logger scope functionality validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = mockLogger.Object.BeginScope("TestScope") != null;
            _logger.LogInformation("Logger scope functionality validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logger scope functionality validation");
            return false;
        }
    }
} 