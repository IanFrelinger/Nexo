using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Service for loading and managing external plugins
    /// </summary>
    public partial class PluginService : IPluginService
    {
        private readonly ILogger<PluginService> _logger;
        private readonly Dictionary<string, IPlugin> _loadedPlugins;
        private readonly Dictionary<string, Assembly> _loadedAssemblies;
        private readonly List<string> _pluginPaths;

        public PluginService(ILogger<PluginService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loadedPlugins = new Dictionary<string, IPlugin>();
            _loadedAssemblies = new Dictionary<string, Assembly>();
            _pluginPaths = new List<string>();
        }

        public async Task<bool> LoadPluginAsync(
            string pluginPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Loading plugin from: {PluginPath}", pluginPath);

                if (!File.Exists(pluginPath))
                {
                    _logger.LogError("Plugin file not found: {PluginPath}", pluginPath);
                    return false;
                }

                // Load assembly
                var assembly = Assembly.LoadFrom(pluginPath);
                _loadedAssemblies[pluginPath] = assembly;

                // Find plugin implementations
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                foreach (var pluginType in pluginTypes)
                {
                    try
                    {
                        var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                        var pluginId = plugin.GetId();

                        if (_loadedPlugins.ContainsKey(pluginId))
                        {
                            _logger.LogWarning("Plugin with ID {PluginId} already loaded, skipping", pluginId);
                            continue;
                        }

                        _loadedPlugins[pluginId] = plugin;
                        _pluginPaths.Add(pluginPath);

                        _logger.LogInformation("Successfully loaded plugin: {PluginId} from {PluginPath}", 
                            pluginId, pluginPath);

                        // Initialize plugin
                        await plugin.InitializeAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to instantiate plugin type {PluginType} from {PluginPath}", 
                            pluginType.Name, pluginPath);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from {PluginPath}", pluginPath);
                return false;
            }
        }

        public async Task<bool> UnloadPluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
                {
                    _logger.LogWarning("Plugin {PluginId} not found for unloading", pluginId);
                    return false;
                }

                // Dispose plugin
                if (plugin is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _loadedPlugins.Remove(pluginId);

                _logger.LogInformation("Successfully unloaded plugin: {PluginId}", pluginId);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unload plugin {PluginId}", pluginId);
                return false;
            }
        }

        public async Task<List<IPlugin>> GetLoadedPluginsAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_loadedPlugins.Values.ToList());
        }

        public async Task<IPlugin?> GetPluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default)
        {
            _loadedPlugins.TryGetValue(pluginId, out var plugin);
            return await Task.FromResult(plugin);
        }

        public async Task<List<IBlock>> GetAvailableBlocksAsync(CancellationToken cancellationToken = default)
        {
            var blocks = new List<IBlock>();

            foreach (var plugin in _loadedPlugins.Values)
            {
                try
                {
                    var pluginBlocks = await plugin.GetBlocksAsync(cancellationToken);
                    blocks.AddRange(pluginBlocks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get blocks from plugin {PluginId}", plugin.GetId());
                }
            }

            return blocks;
        }

        public async Task<IBlock?> GetBlockAsync(
            string blockId,
            CancellationToken cancellationToken = default)
        {
            var blocks = await GetAvailableBlocksAsync(cancellationToken);
            return blocks.FirstOrDefault(b => b.GetId() == blockId);
        }

        public async Task<BlockExecutionResult> ExecuteBlockAsync(
            string blockId,
            Dictionary<string, object> inputs,
            CancellationToken cancellationToken = default)
        {
            var block = await GetBlockAsync(blockId, cancellationToken);
            if (block == null)
            {
                return new BlockExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Block {blockId} not found"
                };
            }

            try
            {
                _logger.LogDebug("Executing block {BlockId}", blockId);
                var result = await block.ExecuteAsync(inputs, cancellationToken);
                
                _logger.LogDebug("Block {BlockId} executed successfully", blockId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute block {BlockId}", blockId);
                return new BlockExecutionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<bool> ValidatePluginAsync(
            string pluginPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(pluginPath))
                {
                    return Task.FromResult(false);
                }

                // Try to load assembly
                var assembly = Assembly.LoadFrom(pluginPath);

                // Check for plugin interfaces
                var hasPluginInterface = assembly.GetTypes()
                    .Any(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                if (!hasPluginInterface)
                {
                    _logger.LogWarning("Assembly {PluginPath} does not contain valid plugin implementations", pluginPath);
                    return Task.FromResult(false);
                }

                // Check for required dependencies
                var references = assembly.GetReferencedAssemblies();
                var hasRequiredDependencies = references.Any(r => r.Name == "Nexo.Core.Domain");

                if (!hasRequiredDependencies)
                {
                    _logger.LogWarning("Assembly {PluginPath} does not reference required dependencies", pluginPath);
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate plugin {PluginPath}", pluginPath);
                return Task.FromResult(false);
            }
        }

        public async Task<PluginInfo> GetPluginInfoAsync(
            string pluginId,
            CancellationToken cancellationToken = default)
        {
            if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                return new PluginInfo { Id = pluginId, IsLoaded = false };
            }

            var blocks = await plugin.GetBlocksAsync(cancellationToken);

            return new PluginInfo
            {
                Id = pluginId,
                Name = plugin.GetName(),
                Version = plugin.GetVersion(),
                Description = plugin.GetDescription(),
                IsLoaded = true,
                BlockCount = blocks.Count,
                BlockIds = blocks.Select(b => b.GetId()).ToList()
            };
        }
    }

    /// <summary>
    /// Plugin interface
    /// </summary>
    public partial interface IPlugin
    {
        string GetId();
        string GetName();
        string GetVersion();
        string GetDescription();
        Task InitializeAsync(CancellationToken cancellationToken = default);
        Task<List<IBlock>> GetBlocksAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Block interface
    /// </summary>
    public partial interface IBlock
    {
        string GetId();
        string GetName();
        string GetDescription();
        List<BlockInput> GetInputs();
        List<BlockOutput> GetOutputs();
        Task<BlockExecutionResult> ExecuteAsync(
            Dictionary<string, object> inputs,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Block input definition
    /// </summary>
    public partial class BlockInput
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; } = true;
        public string Description { get; set; } = string.Empty;
        public object? DefaultValue { get; set; }
    }

    /// <summary>
    /// Block output definition
    /// </summary>
    public partial class BlockOutput
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Block execution result
    /// </summary>
    public partial class BlockExecutionResult
    {
        public bool Success { get; set; }
        public Dictionary<string, object> Outputs { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// Plugin information
    /// </summary>
    public partial class PluginInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsLoaded { get; set; }
        public int BlockCount { get; set; }
        public List<string> BlockIds { get; set; } = new();
    }

    /// <summary>
    /// Interface for plugin service
    /// </summary>
    public partial interface IPluginService
    {
        Task<bool> LoadPluginAsync(
            string pluginPath,
            CancellationToken cancellationToken = default);

        Task<bool> UnloadPluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default);

        Task<List<IPlugin>> GetLoadedPluginsAsync(CancellationToken cancellationToken = default);

        Task<IPlugin?> GetPluginAsync(
            string pluginId,
            CancellationToken cancellationToken = default);

        Task<List<IBlock>> GetAvailableBlocksAsync(CancellationToken cancellationToken = default);

        Task<IBlock?> GetBlockAsync(
            string blockId,
            CancellationToken cancellationToken = default);

        Task<BlockExecutionResult> ExecuteBlockAsync(
            string blockId,
            Dictionary<string, object> inputs,
            CancellationToken cancellationToken = default);

        Task<bool> ValidatePluginAsync(
            string pluginPath,
            CancellationToken cancellationToken = default);

        Task<PluginInfo> GetPluginInfoAsync(
            string pluginId,
            CancellationToken cancellationToken = default);
    }
}
