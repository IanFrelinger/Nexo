using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Contracts;
using Nexo.Core.Contracts.Capabilities;
using Nexo.PluginHost.Abstractions;
using Nexo.PluginHost.Schemas;

namespace Nexo.PluginHost
{
    /// <summary>
    /// A hardened plugin host that uses collectible AssemblyLoadContext for safe plugin loading and unloading.
    /// </summary>
    public class PluginHost : IDisposable
    {
        private readonly ILogger<PluginHost> _logger;
        private readonly Dictionary<string, PluginContext> _loadedPlugins;
        private readonly string _pluginDependenciesPath;
        private readonly string _curatedLibsPath;
        private readonly INexoFileSystem _fileSystem;
        private readonly INexoProcessRunner _processRunner;
        private readonly ActivitySource _activitySource;
        private bool _disposed;

        public PluginHost(
            ILogger<PluginHost> logger, 
            string pluginDependenciesPath = null,
            string curatedLibsPath = null,
            INexoFileSystem fileSystem = null,
            INexoProcessRunner processRunner = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loadedPlugins = new Dictionary<string, PluginContext>();
            _pluginDependenciesPath = pluginDependenciesPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugin-dependencies");
            _curatedLibsPath = curatedLibsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "curated-libs");
            _fileSystem = fileSystem ?? new DefaultFileSystem();
            _processRunner = processRunner ?? new DefaultProcessRunner();
            _activitySource = new ActivitySource("Nexo.PluginHost");
        }

        /// <summary>
        /// Loads a plugin from the specified path with capability validation.
        /// </summary>
        /// <param name="pluginPath">Path to the plugin assembly</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the plugin was loaded successfully, false otherwise</returns>
        public async Task<bool> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PluginHost));

            using var activity = _activitySource.StartActivity("LoadPlugin");
            activity?.SetTag("plugin.path", pluginPath);
            
            var startTime = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("Starting plugin load operation from: {PluginPath}", pluginPath);

                if (!_fileSystem.FileExists(pluginPath))
                {
                    _logger.LogError("Plugin file not found: {PluginPath}", pluginPath);
                    activity?.SetStatus(ActivityStatusCode.Error, "Plugin file not found");
                    return false;
                }

                // Load plugin manifest
                var manifestPath = _fileSystem.ChangeExtension(pluginPath, ".json");
                var manifest = await LoadPluginManifestAsync(manifestPath, cancellationToken);
                if (manifest == null)
                {
                    _logger.LogError("Failed to load plugin manifest: {ManifestPath}", manifestPath);
                    activity?.SetStatus(ActivityStatusCode.Error, "Failed to load plugin manifest");
                    return false;
                }

                activity?.SetTag("plugin.name", manifest.Name);
                activity?.SetTag("plugin.version", manifest.Version);
                activity?.SetTag("plugin.capabilities", string.Join(",", manifest.Capabilities));

                // Create collectible AssemblyLoadContext
                var context = new PluginAssemblyLoadContext(pluginPath, _pluginDependenciesPath, _curatedLibsPath, _logger as ILogger<PluginAssemblyLoadContext>);
                
                // Load the assembly
                var assembly = context.LoadPluginAssembly();
                
                // Validate capabilities
                var capabilityTypes = ValidatePluginCapabilities(assembly, manifest);
                if (capabilityTypes.Count == 0)
                {
                    _logger.LogError("Plugin {PluginName} does not implement any declared capabilities", manifest.Name);
                    context.UnloadAndWait();
                    activity?.SetStatus(ActivityStatusCode.Error, "No valid capabilities found");
                    return false;
                }

                // Validate constructor dependencies
                if (!ValidateConstructorDependencies(capabilityTypes))
                {
                    _logger.LogError("Plugin {PluginName} has invalid constructor dependencies", manifest.Name);
                    context.UnloadAndWait();
                    activity?.SetStatus(ActivityStatusCode.Error, "Invalid constructor dependencies");
                    return false;
                }

                // Create plugin instances
                var pluginInstances = new List<object>();
                foreach (var capabilityType in capabilityTypes)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(capabilityType);
                        if (instance != null)
                        {
                            pluginInstances.Add(instance);
                            _logger.LogInformation("Created instance of capability: {CapabilityType}", capabilityType.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create instance of capability: {CapabilityType}", capabilityType.Name);
                    }
                }

                if (pluginInstances.Count == 0)
                {
                    _logger.LogError("No valid capability instances could be created for plugin: {PluginName}", manifest.Name);
                    context.UnloadAndWait();
                    activity?.SetStatus(ActivityStatusCode.Error, "No valid instances created");
                    return false;
                }

                // Store plugin context
                var pluginContext = new PluginContext
                {
                    Manifest = manifest,
                    Assembly = assembly,
                    LoadContext = context,
                    Instances = pluginInstances,
                    LoadTime = DateTime.UtcNow
                };

                _loadedPlugins[manifest.Name] = pluginContext;

                var loadDuration = DateTime.UtcNow - startTime;
                activity?.SetTag("load.duration.ms", loadDuration.TotalMilliseconds);
                activity?.SetTag("capability.count", pluginInstances.Count);
                
                _logger.LogInformation("Successfully loaded plugin: {PluginName} v{Version} with {CapabilityCount} capabilities in {LoadDuration}ms", 
                    manifest.Name, manifest.Version, pluginInstances.Count, loadDuration.TotalMilliseconds);

                // Audit log
                _logger.LogInformation("PLUGIN_AUDIT: LOADED - Name: {PluginName}, Version: {Version}, Capabilities: [{Capabilities}], LoadTime: {LoadTime}, Duration: {Duration}ms",
                    manifest.Name, manifest.Version, string.Join(", ", manifest.Capabilities), startTime, loadDuration.TotalMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                var loadDuration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Failed to load plugin from: {PluginPath} after {LoadDuration}ms", pluginPath, loadDuration.TotalMilliseconds);
                
                // Audit log for failure
                _logger.LogError("PLUGIN_AUDIT: LOAD_FAILED - Path: {PluginPath}, Error: {Error}, LoadTime: {LoadTime}, Duration: {Duration}ms",
                    pluginPath, ex.Message, startTime, loadDuration.TotalMilliseconds);
                
                return false;
            }
        }

        /// <summary>
        /// Validates that plugin constructors only request allowed interfaces.
        /// </summary>
        private bool ValidateConstructorDependencies(List<Type> capabilityTypes)
        {
            var allowedInterfaces = new HashSet<Type>
            {
                typeof(INexoFileSystem),
                typeof(INexoProcessRunner),
                typeof(ILogger<>)
            };

            foreach (var type in capabilityTypes)
            {
                var constructors = type.GetConstructors();
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        var parameterType = parameter.ParameterType;
                        
                        // Check if it's a generic type (like ILogger<T>)
                        if (parameterType.IsGenericType)
                        {
                            var genericTypeDefinition = parameterType.GetGenericTypeDefinition();
                            if (!allowedInterfaces.Contains(genericTypeDefinition))
                            {
                                _logger.LogWarning("Constructor parameter {ParameterType} is not in allowed interfaces", parameterType.Name);
                                return false;
                            }
                        }
                        else if (!allowedInterfaces.Contains(parameterType))
                        {
                            _logger.LogWarning("Constructor parameter {ParameterType} is not in allowed interfaces", parameterType.Name);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Unloads a plugin by name.
        /// </summary>
        /// <param name="pluginName">Name of the plugin to unload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the plugin was unloaded successfully, false otherwise</returns>
        public async Task<bool> UnloadPluginAsync(string pluginName, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PluginHost));

            using var activity = _activitySource.StartActivity("UnloadPlugin");
            activity?.SetTag("plugin.name", pluginName);
            
            var startTime = DateTime.UtcNow;
            if (!_loadedPlugins.TryGetValue(pluginName, out var pluginContext))
            {
                _logger.LogWarning("Plugin not found for unloading: {PluginName}", pluginName);
                activity?.SetStatus(ActivityStatusCode.Error, "Plugin not found");
                return false;
            }

            try
            {
                _logger.LogInformation("Starting plugin unload operation: {PluginName}", pluginName);

                // Dispose instances if they implement IDisposable
                foreach (var instance in pluginContext.Instances)
                {
                    if (instance is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                // Unload the AssemblyLoadContext
                if (pluginContext.LoadContext is PluginAssemblyLoadContext pluginLoadContext)
                {
                    pluginLoadContext.UnloadAndWait();
                }
                else
                {
                    pluginContext.LoadContext.Unload();
                }

                // Remove from loaded plugins
                _loadedPlugins.Remove(pluginName);

                var unloadDuration = DateTime.UtcNow - startTime;
                activity?.SetTag("unload.duration.ms", unloadDuration.TotalMilliseconds);
                _logger.LogInformation("Successfully unloaded plugin: {PluginName} in {UnloadDuration}ms", pluginName, unloadDuration.TotalMilliseconds);

                // Audit log
                _logger.LogInformation("PLUGIN_AUDIT: UNLOADED - Name: {PluginName}, Version: {Version}, UnloadTime: {UnloadTime}, Duration: {Duration}ms",
                    pluginName, pluginContext.Manifest.Version, startTime, unloadDuration.TotalMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                var unloadDuration = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Failed to unload plugin: {PluginName} after {UnloadDuration}ms", pluginName, unloadDuration.TotalMilliseconds);
                
                // Audit log for failure
                _logger.LogError("PLUGIN_AUDIT: UNLOAD_FAILED - Name: {PluginName}, Error: {Error}, UnloadTime: {UnloadTime}, Duration: {Duration}ms",
                    pluginName, ex.Message, startTime, unloadDuration.TotalMilliseconds);
                
                return false;
            }
        }

        /// <summary>
        /// Gets all loaded plugin names.
        /// </summary>
        public IEnumerable<string> GetLoadedPluginNames()
        {
            return _loadedPlugins.Keys.ToList();
        }

        /// <summary>
        /// Gets plugin instances of a specific capability type.
        /// </summary>
        /// <typeparam name="T">The capability interface type</typeparam>
        /// <returns>Collection of plugin instances implementing the capability</returns>
        public IEnumerable<T> GetCapabilityInstances<T>() where T : class
        {
            return _loadedPlugins.Values
                .SelectMany(pc => pc.Instances)
                .OfType<T>();
        }

        /// <summary>
        /// Gets a specific plugin context by name.
        /// </summary>
        /// <param name="pluginName">Name of the plugin</param>
        /// <returns>Plugin context or null if not found</returns>
        public PluginContext GetPluginContext(string pluginName)
        {
            _loadedPlugins.TryGetValue(pluginName, out var context);
            return context;
        }

        private async Task<PluginManifest> LoadPluginManifestAsync(string manifestPath, CancellationToken cancellationToken)
        {
            try
            {
                if (!_fileSystem.FileExists(manifestPath))
                {
                    _logger.LogWarning("Plugin manifest not found: {ManifestPath}", manifestPath);
                    return null;
                }

                var manifestJson = await _fileSystem.ReadAllTextAsync(manifestPath, cancellationToken);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson);
                
                if (manifest == null || string.IsNullOrEmpty(manifest.Name))
                {
                    _logger.LogError("Invalid plugin manifest: {ManifestPath}", manifestPath);
                    return null;
                }

                return manifest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin manifest: {ManifestPath}", manifestPath);
                return null;
            }
        }

        private List<Type> ValidatePluginCapabilities(Assembly assembly, PluginManifest manifest)
        {
            var capabilityTypes = new List<Type>();
            var capabilityInterfaces = new[]
            {
                typeof(ISense),
                typeof(IDecide),
                typeof(IAct),
                typeof(IGuard)
            };

            var allTypes = assembly.GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract && t.IsClass)
                .ToList();

            foreach (var type in allTypes)
            {
                var implementedCapabilities = capabilityInterfaces
                    .Where(ci => ci.IsAssignableFrom(type))
                    .ToList();

                if (implementedCapabilities.Count > 0)
                {
                    // Verify that the declared capabilities match the implemented ones
                    var declaredCapabilities = manifest.Capabilities.ToHashSet();
                    var implementedCapabilityNames = implementedCapabilities
                        .Select(ci => ci.Name)
                        .ToHashSet();

                    if (declaredCapabilities.SetEquals(implementedCapabilityNames))
                    {
                        capabilityTypes.Add(type);
                        _logger.LogInformation("Validated capability implementation: {TypeName} implements {Capabilities}", 
                            type.Name, string.Join(", ", implementedCapabilityNames));
                    }
                    else
                    {
                        _logger.LogWarning("Capability mismatch for type {TypeName}. Declared: [{Declared}], Implemented: [{Implemented}]", 
                            type.Name, string.Join(", ", declaredCapabilities), string.Join(", ", implementedCapabilityNames));
                    }
                }
            }

            return capabilityTypes;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Unload all plugins
            var pluginNames = _loadedPlugins.Keys.ToList();
            foreach (var pluginName in pluginNames)
            {
                try
                {
                    UnloadPluginAsync(pluginName).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing plugin: {PluginName}", pluginName);
                }
            }

            _loadedPlugins.Clear();
        }
    }

    /// <summary>
    /// Represents a loaded plugin context.
    /// </summary>
    public class PluginContext
    {
        public PluginManifest Manifest { get; set; }
        public Assembly Assembly { get; set; }
        public AssemblyLoadContext LoadContext { get; set; }
        public List<object> Instances { get; set; }
        public DateTime LoadTime { get; set; }
    }

    /// <summary>
    /// A collectible AssemblyLoadContext for loading plugins.
    /// </summary>
    public class CollectiblePluginLoadContext : AssemblyLoadContext
    {
        private readonly string _pluginPath;
        private readonly string _dependenciesPath;

        public CollectiblePluginLoadContext(string pluginPath, string dependenciesPath) : base(isCollectible: true)
        {
            _pluginPath = pluginPath;
            _dependenciesPath = dependenciesPath;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // First try to load from dependencies folder
            if (!string.IsNullOrEmpty(_dependenciesPath) && Directory.Exists(_dependenciesPath))
            {
                var dependencyPath = Path.Combine(_dependenciesPath, $"{assemblyName.Name}.dll");
                if (File.Exists(dependencyPath))
                {
                    return LoadFromAssemblyPath(dependencyPath);
                }
            }

            // Fall back to default loading behavior
            return null;
        }
    }
}
