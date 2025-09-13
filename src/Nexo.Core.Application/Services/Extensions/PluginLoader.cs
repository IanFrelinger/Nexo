using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Models.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Extensions
{
    public class PluginLoader : IPluginLoader
    {
        private readonly ILogger<PluginLoader> _logger;
        private readonly ConcurrentDictionary<string, IPlugin> _loadedPlugins;
        private readonly ConcurrentDictionary<string, Assembly> _loadedAssemblies;

        public PluginLoader(ILogger<PluginLoader> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loadedPlugins = new ConcurrentDictionary<string, IPlugin>();
            _loadedAssemblies = new ConcurrentDictionary<string, Assembly>();
        }

        public Task<PluginLoadResult> LoadPluginAsync(byte[] assemblyBytes, string pluginName)
        {
            var result = new PluginLoadResult
            {
                PluginName = pluginName,
                LoadTime = TimeSpan.Zero
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Validate inputs
                if (assemblyBytes == null || assemblyBytes.Length == 0)
                {
                    result.AddPluginError("Assembly bytes cannot be empty", "assemblyBytes", "PL0001");
                    return Task.FromResult(result);
                }

                if (string.IsNullOrWhiteSpace(pluginName))
                {
                    result.AddPluginError("Plugin name cannot be empty", "pluginName", "PL0002");
                    return Task.FromResult(result);
                }

                // Check if plugin is already loaded
                if (_loadedPlugins.ContainsKey(pluginName))
                {
                    result.AddPluginWarning($"Plugin '{pluginName}' is already loaded", "pluginName", "PL0003");
                    result.Plugin = _loadedPlugins[pluginName];
                    return Task.FromResult(result);
                }

                // Load assembly from bytes
                Assembly assembly;
                try
                {
                    assembly = Assembly.Load(assemblyBytes);
                    _loadedAssemblies[pluginName] = assembly;
                }
                catch (Exception ex)
                {
                    result.AddPluginError($"Failed to load assembly: {ex.Message}", "assemblyBytes", "PL0004");
                    _logger.LogError(ex, "Failed to load assembly for plugin: {PluginName}", pluginName);
                    return Task.FromResult(result);
                }

                // Find IPlugin implementation
                var pluginType = FindPluginType(assembly);
                _logger.LogInformation("Found plugin type: {PluginType}", pluginType?.Name ?? "null");
                if (pluginType == null)
                {
                    result.AddPluginError("No IPlugin implementation found in assembly", "assembly", "PL0005");
                    return Task.FromResult(result);
                }

                // Create plugin instance
                IPlugin? plugin;
                try
                {
                    plugin = (IPlugin?)Activator.CreateInstance(pluginType);
                    if (plugin == null)
                    {
                        result.AddPluginError("Failed to create plugin instance", "pluginType", "PL0006");
                        return Task.FromResult(result);
                    }
                }
                catch (Exception ex)
                {
                    result.AddPluginError($"Failed to create plugin instance: {ex.Message}", "pluginType", "PL0007");
                    _logger.LogError(ex, "Failed to create plugin instance for: {PluginName}", pluginName);
                    return Task.FromResult(result);
                }

                // Store loaded plugin
                _loadedPlugins[pluginName] = plugin;
                result.Plugin = plugin;
                result.PluginVersion = plugin.Version ?? "1.0.0";
                result.IsSuccess = true;

                _logger.LogInformation("Successfully loaded plugin: {PluginName} v{PluginVersion}", 
                    pluginName, result.PluginVersion);

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                result.AddPluginError($"Unexpected error loading plugin: {ex.Message}", "general", "PL0008");
                _logger.LogError(ex, "Unexpected error loading plugin: {PluginName}", pluginName);
                return Task.FromResult(result);
            }
            finally
            {
                stopwatch.Stop();
                result.LoadTime = stopwatch.Elapsed;
            }
        }

        public Task<PluginLoadResult> LoadPluginAsync(string assemblyPath, string pluginName)
        {
            var result = new PluginLoadResult
            {
                PluginName = pluginName,
                LoadTime = TimeSpan.Zero
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(assemblyPath))
                {
                    result.AddPluginError("Assembly path cannot be empty", "assemblyPath", "PL0009");
                    return Task.FromResult(result);
                }

                if (string.IsNullOrWhiteSpace(pluginName))
                {
                    result.AddPluginError("Plugin name cannot be empty", "pluginName", "PL0010");
                    return Task.FromResult(result);
                }

                if (!File.Exists(assemblyPath))
                {
                    result.AddPluginError($"Assembly file not found: {assemblyPath}", "assemblyPath", "PL0011");
                    return Task.FromResult(result);
                }

                // Load assembly from file
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(assemblyPath);
                    _loadedAssemblies[pluginName] = assembly;
                }
                catch (Exception ex)
                {
                    result.AddPluginError($"Failed to load assembly from file: {ex.Message}", "assemblyPath", "PL0012");
                    _logger.LogError(ex, "Failed to load assembly from file: {AssemblyPath}", assemblyPath);
                    return Task.FromResult(result);
                }

                // Find IPlugin implementation
                var pluginType = FindPluginType(assembly);
                if (pluginType == null)
                {
                    result.AddPluginError("No IPlugin implementation found in assembly", "assembly", "PL0013");
                    return Task.FromResult(result);
                }

                // Create plugin instance
                IPlugin? plugin;
                try
                {
                    plugin = (IPlugin?)Activator.CreateInstance(pluginType);
                    if (plugin == null)
                    {
                        result.AddPluginError("Failed to create plugin instance", "pluginType", "PL0014");
                        return Task.FromResult(result);
                    }
                }
                catch (Exception ex)
                {
                    result.AddPluginError($"Failed to create plugin instance: {ex.Message}", "pluginType", "PL0015");
                    _logger.LogError(ex, "Failed to create plugin instance for: {PluginName}", pluginName);
                    return Task.FromResult(result);
                }

                // Store loaded plugin
                _loadedPlugins[pluginName] = plugin;
                result.Plugin = plugin;
                result.PluginVersion = plugin.Version ?? "1.0.0";
                result.IsSuccess = true;

                _logger.LogInformation("Successfully loaded plugin from file: {PluginName} v{PluginVersion} from {AssemblyPath}", 
                    pluginName, result.PluginVersion, assemblyPath);

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                result.AddPluginError($"Unexpected error loading plugin from file: {ex.Message}", "general", "PL0016");
                _logger.LogError(ex, "Unexpected error loading plugin from file: {PluginName} from {AssemblyPath}", pluginName, assemblyPath);
                return Task.FromResult(result);
            }
            finally
            {
                stopwatch.Stop();
                result.LoadTime = stopwatch.Elapsed;
            }
        }

        public Task<bool> UnloadPluginAsync(string pluginName)
        {
            if (string.IsNullOrWhiteSpace(pluginName))
            {
                return Task.FromResult(false);
            }

            var removed = _loadedPlugins.TryRemove(pluginName, out _);
            _loadedAssemblies.TryRemove(pluginName, out _);

            if (removed)
            {
                _logger.LogInformation("Successfully unloaded plugin: {PluginName}", pluginName);
            }

            return Task.FromResult(removed);
        }

        public Task<IPlugin?> GetPluginAsync(string pluginName)
        {
            if (string.IsNullOrWhiteSpace(pluginName))
            {
                return Task.FromResult<IPlugin?>(null);
            }

            _loadedPlugins.TryGetValue(pluginName, out var plugin);
            return Task.FromResult(plugin);
        }

        public Task<bool> IsPluginLoadedAsync(string pluginName)
        {
            if (string.IsNullOrWhiteSpace(pluginName))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(_loadedPlugins.ContainsKey(pluginName));
        }

        private Type? FindPluginType(Assembly assembly)
        {
            try
            {
                var types = assembly.GetTypes();
                _logger.LogInformation("Assembly has {TypeCount} types", types.Length);
                foreach (var type in types)
                {
                    _logger.LogInformation("Type: {TypeName}, Implements IPlugin: {ImplementsIPlugin}, IsInterface: {IsInterface}, IsAbstract: {IsAbstract}", 
                        type.Name, typeof(IPlugin).IsAssignableFrom(type), type.IsInterface, type.IsAbstract);
                }
                return types.FirstOrDefault(t => 
                    typeof(IPlugin).IsAssignableFrom(t) && 
                    !t.IsInterface && 
                    !t.IsAbstract);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding plugin type in assembly");
                return null;
            }
        }
    }
}
