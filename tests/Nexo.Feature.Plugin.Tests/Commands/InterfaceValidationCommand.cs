using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using System;
using System.Linq;

namespace Nexo.Feature.Plugin.Tests.Commands;

/// <summary>
/// Command for validating Plugin interfaces with proper logging and timeouts.
/// </summary>
public class InterfaceValidationCommand
{
    private readonly ILogger<InterfaceValidationCommand> _logger;

    public InterfaceValidationCommand(ILogger<InterfaceValidationCommand> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates IPlugin interface structure.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateIPluginInterface(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting IPlugin interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that IPlugin interface exists and has expected properties and methods
            var pluginType = typeof(IPlugin);
            var properties = pluginType.GetProperties();
            var methods = pluginType.GetMethods();
            
            var hasNameProperty = properties.Any(p => p.Name == "Name" && p.PropertyType == typeof(string));
            var hasVersionProperty = properties.Any(p => p.Name == "Version" && p.PropertyType == typeof(string));
            var hasDescriptionProperty = properties.Any(p => p.Name == "Description" && p.PropertyType == typeof(string));
            var hasAuthorProperty = properties.Any(p => p.Name == "Author" && p.PropertyType == typeof(string));
            var hasIsEnabledProperty = properties.Any(p => p.Name == "IsEnabled" && p.PropertyType == typeof(bool));
            
            var hasInitializeMethod = methods.Any(m => m.Name == "InitializeAsync");
            var hasShutdownMethod = methods.Any(m => m.Name == "ShutdownAsync");

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("IPlugin interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = hasNameProperty && hasVersionProperty && hasDescriptionProperty && 
                        hasAuthorProperty && hasIsEnabledProperty && hasInitializeMethod && hasShutdownMethod;
            _logger.LogInformation("IPlugin interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during IPlugin interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates IPluginManager interface structure.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateIPluginManagerInterface(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting IPluginManager interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that IPluginManager interface exists and has expected methods
            var managerType = typeof(IPluginManager);
            var methods = managerType.GetMethods();
            
            var hasLoadPluginsMethod = methods.Any(m => m.Name == "LoadPluginsAsync");
            var hasGetPluginsMethod = methods.Any(m => m.Name == "GetPlugins");
            var hasGetPluginsGenericMethod = methods.Any(m => m.Name == "GetPlugins" && m.IsGenericMethod);
            var hasGetPluginMethod = methods.Any(m => m.Name == "GetPlugin" && m.IsGenericMethod);
            var hasRegisterPluginMethod = methods.Any(m => m.Name == "RegisterPluginAsync");
            var hasUnregisterPluginMethod = methods.Any(m => m.Name == "UnregisterPluginAsync");
            var hasEnablePluginMethod = methods.Any(m => m.Name == "EnablePluginAsync");
            var hasDisablePluginMethod = methods.Any(m => m.Name == "DisablePluginAsync");
            var hasReloadPluginsMethod = methods.Any(m => m.Name == "ReloadPluginsAsync");
            var hasShutdownMethod = methods.Any(m => m.Name == "ShutdownAsync");

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("IPluginManager interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = hasLoadPluginsMethod && hasGetPluginsMethod && hasGetPluginsGenericMethod && 
                        hasGetPluginMethod && hasRegisterPluginMethod && hasUnregisterPluginMethod &&
                        hasEnablePluginMethod && hasDisablePluginMethod && hasReloadPluginsMethod && hasShutdownMethod;
            _logger.LogInformation("IPluginManager interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during IPluginManager interface validation");
            return false;
        }
    }

    /// <summary>
    /// Validates IPluginLoader interface structure.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 3000)</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public bool ValidateIPluginLoaderInterface(int timeoutMs = 3000)
    {
        _logger.LogInformation("Starting IPluginLoader interface validation");
        
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Validate that IPluginLoader interface exists and has expected methods
            var loaderType = typeof(IPluginLoader);
            var methods = loaderType.GetMethods();
            
            var hasLoadPluginsMethod = methods.Any(m => m.Name == "LoadPluginsAsync");
            var hasLoadPluginMethod = methods.Any(m => m.Name == "LoadPluginAsync" && m.IsGenericMethod);
            var hasGetPluginDirectoryMethod = methods.Any(m => m.Name == "GetPluginDirectory");
            var hasPluginExistsMethod = methods.Any(m => m.Name == "PluginExistsAsync");

            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed.TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("IPluginLoader interface validation exceeded timeout: {ElapsedMs}ms", elapsed.TotalMilliseconds);
                return false;
            }

            var result = hasLoadPluginsMethod && hasLoadPluginMethod && hasGetPluginDirectoryMethod && hasPluginExistsMethod;
            _logger.LogInformation("IPluginLoader interface validation completed: {Result}", result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during IPluginLoader interface validation");
            return false;
        }
    }
} 