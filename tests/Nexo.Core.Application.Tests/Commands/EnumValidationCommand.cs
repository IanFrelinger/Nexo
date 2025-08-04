using Microsoft.Extensions.Logging;
using Nexo.Shared.Interfaces.Resource;
using Nexo.Core.Application.Interfaces.Caching;
using System;

namespace Nexo.Core.Application.Tests.Commands;

/// <summary>
/// Command for validating Core.Application enums with proper logging and timeouts.
/// </summary>
public class EnumValidationCommand
{
    private readonly ILogger<EnumValidationCommand> _logger;

    public EnumValidationCommand(ILogger<EnumValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates ResourceType enum values.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateResourceType(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ResourceType enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var cpu = Enum.IsDefined(typeof(ResourceType), ResourceType.CPU);
            var memory = Enum.IsDefined(typeof(ResourceType), ResourceType.Memory);
            var storage = Enum.IsDefined(typeof(ResourceType), ResourceType.Storage);
            var network = Enum.IsDefined(typeof(ResourceType), ResourceType.Network);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ResourceType validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = cpu && memory && storage && network;
            _logger.LogInformation("ResourceType enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ResourceType enum validation");
            return false;
        }
    }

    /// <summary>
    /// Validates ResourcePriority enum values.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateResourcePriority(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ResourcePriority enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var low = Enum.IsDefined(typeof(ResourcePriority), ResourcePriority.Low);
            var normal = Enum.IsDefined(typeof(ResourcePriority), ResourcePriority.Normal);
            var high = Enum.IsDefined(typeof(ResourcePriority), ResourcePriority.High);
            var critical = Enum.IsDefined(typeof(ResourcePriority), ResourcePriority.Critical);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ResourcePriority validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = low && normal && high && critical;
            _logger.LogInformation("ResourcePriority enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ResourcePriority enum validation");
            return false;
        }
    }

    /// <summary>
    /// Validates ResourceAlertType enum values.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateResourceAlertType(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ResourceAlertType enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var highUtilization = Enum.IsDefined(typeof(ResourceAlertType), ResourceAlertType.HighUtilization);
            var resourceExhaustion = Enum.IsDefined(typeof(ResourceAlertType), ResourceAlertType.ResourceExhaustion);
            var allocationFailure = Enum.IsDefined(typeof(ResourceAlertType), ResourceAlertType.AllocationFailure);
            var providerHealth = Enum.IsDefined(typeof(ResourceAlertType), ResourceAlertType.ProviderHealth);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ResourceAlertType validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = highUtilization && resourceExhaustion && allocationFailure && providerHealth;
            _logger.LogInformation("ResourceAlertType enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ResourceAlertType enum validation");
            return false;
        }
    }

    /// <summary>
    /// Validates ResourceAlertSeverity enum values.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateResourceAlertSeverity(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ResourceAlertSeverity enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var information = Enum.IsDefined(typeof(ResourceAlertSeverity), ResourceAlertSeverity.Information);
            var warning = Enum.IsDefined(typeof(ResourceAlertSeverity), ResourceAlertSeverity.Warning);
            var error = Enum.IsDefined(typeof(ResourceAlertSeverity), ResourceAlertSeverity.Error);
            var critical = Enum.IsDefined(typeof(ResourceAlertSeverity), ResourceAlertSeverity.Critical);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ResourceAlertSeverity validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = information && warning && error && critical;
            _logger.LogInformation("ResourceAlertSeverity enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ResourceAlertSeverity enum validation");
            return false;
        }
    }

    /// <summary>
    /// Validates ResourceHealth enum values.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateResourceHealth(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting ResourceHealth enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var healthy = Enum.IsDefined(typeof(ResourceHealth), ResourceHealth.Healthy);
            var degraded = Enum.IsDefined(typeof(ResourceHealth), ResourceHealth.Degraded);
            var unhealthy = Enum.IsDefined(typeof(ResourceHealth), ResourceHealth.Unhealthy);
            var unknown = Enum.IsDefined(typeof(ResourceHealth), ResourceHealth.Unknown);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("ResourceHealth validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = healthy && degraded && unhealthy && unknown;
            _logger.LogInformation("ResourceHealth enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ResourceHealth enum validation");
            return false;
        }
    }

    /// <summary>
    /// Validates CacheItemPriority enum values.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateCacheItemPriority(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting CacheItemPriority enum validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            var low = Enum.IsDefined(typeof(CacheItemPriority), CacheItemPriority.Low);
            var normal = Enum.IsDefined(typeof(CacheItemPriority), CacheItemPriority.Normal);
            var high = Enum.IsDefined(typeof(CacheItemPriority), CacheItemPriority.High);
            var neverRemove = Enum.IsDefined(typeof(CacheItemPriority), CacheItemPriority.NeverRemove);

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("CacheItemPriority validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = low && normal && high && neverRemove;
            _logger.LogInformation("CacheItemPriority enum validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CacheItemPriority enum validation");
            return false;
        }
    }
} 