using Microsoft.Extensions.Logging;
using NexoLogger = Nexo.Core.Application.Interfaces.Logging.ILogger;
using NexoLoggerFactory = Nexo.Core.Application.Interfaces.Logging.ILoggerFactory;
using NexoLoggerProvider = Nexo.Core.Application.Interfaces.Logging.ILoggerProvider;
using System;
using System.Linq;

namespace Nexo.Feature.Logging.Tests.Commands;

/// <summary>
/// Command for validating Logging interfaces with proper logging and timeouts.
/// </summary>
public class InterfaceValidationCommand
{
    private readonly ILogger<InterfaceValidationCommand> _logger;

    public InterfaceValidationCommand(ILogger<InterfaceValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates ILogger interface structure.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateILoggerInterface(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ILogger interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that ILogger interface exists and has expected methods
            var loggerType = typeof(NexoLogger);
            var methods = loggerType.GetMethods();
            
            var hasLogMethod = methods.Any(m => m.Name == "Log");
            var hasIsEnabledMethod = methods.Any(m => m.Name == "IsEnabled");
            var hasBeginScopeMethod = methods.Any(m => m.Name == "BeginScope");

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ILogger interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = hasLogMethod && hasIsEnabledMethod && hasBeginScopeMethod;
            _logger.LogInformation("ILogger interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ILogger interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates ILogger{T} interface structure.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateILoggerGenericInterface(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ILogger<T> interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that ILogger<T> interface exists and inherits from ILogger
            var loggerGenericType = typeof(Nexo.Core.Application.Interfaces.Logging.ILogger<>);
            var baseType = loggerGenericType.GetInterfaces();
            
            var inheritsFromILogger = baseType.Any(t => t == typeof(NexoLogger));
            var isGenericType = loggerGenericType.IsGenericType;

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ILogger<T> interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = inheritsFromILogger && isGenericType;
            _logger.LogInformation("ILogger<T> interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ILogger<T> interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates ILoggerFactory interface structure.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateILoggerFactoryInterface(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ILoggerFactory interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that ILoggerFactory interface exists and has expected methods
            var factoryType = typeof(NexoLoggerFactory);
            var methods = factoryType.GetMethods();
            
            var hasCreateLoggerMethod = methods.Any(m => m.Name == "CreateLogger");
            var hasAddProviderMethod = methods.Any(m => m.Name == "AddProvider");
            var hasDisposeMethod = methods.Any(m => m.Name == "Dispose");

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ILoggerFactory interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = hasCreateLoggerMethod && hasAddProviderMethod && hasDisposeMethod;
            _logger.LogInformation("ILoggerFactory interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ILoggerFactory interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates ILoggerProvider interface structure.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateILoggerProviderInterface(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ILoggerProvider interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that ILoggerProvider interface exists and has expected methods
            var providerType = typeof(NexoLoggerProvider);
            var methods = providerType.GetMethods();
            var interfaces = providerType.GetInterfaces();
            
            var hasCreateLoggerMethod = methods.Any(m => m.Name == "CreateLogger");
            var implementsIDisposable = interfaces.Any(i => i == typeof(IDisposable));

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ILoggerProvider interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = hasCreateLoggerMethod && implementsIDisposable;
            _logger.LogInformation("ILoggerProvider interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ILoggerProvider interface validation");
            return false;
        }
    }
} 