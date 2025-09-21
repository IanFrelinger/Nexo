using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.PluginHost;
using Nexo.Core.Contracts.Capabilities;

namespace Examples
{
    /// <summary>
    /// Example demonstrating how to use the hardened PluginHost system.
    /// </summary>
    public class PluginHostUsageExample
    {
        private readonly ILogger<PluginHost> _logger;

        public PluginHostUsageExample(ILogger<PluginHost> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Demonstrates loading, using, and unloading a plugin with capability validation.
        /// </summary>
        public async Task DemonstratePluginHosting()
        {
            // Create plugin host with controlled dependencies folder
            var dependenciesPath = "plugin-dependencies";
            using var pluginHost = new PluginHost(_logger, dependenciesPath);

            try
            {
                // Load a plugin (assumes TestPlugin.dll and TestPlugin.json exist)
                var pluginPath = "test-plugins/TestPlugin/bin/Debug/net8.0/TestPlugin.dll";
                var loadSuccess = await pluginHost.LoadPluginAsync(pluginPath);

                if (!loadSuccess)
                {
                    _logger.LogError("Failed to load plugin from: {PluginPath}", pluginPath);
                    return;
                }

                _logger.LogInformation("Plugin loaded successfully!");

                // Get plugin context for inspection
                var context = pluginHost.GetPluginContext("TestPlugin");
                if (context != null)
                {
                    _logger.LogInformation("Plugin Details:");
                    _logger.LogInformation("  Name: {Name}", context.Manifest.Name);
                    _logger.LogInformation("  Version: {Version}", context.Manifest.Version);
                    _logger.LogInformation("  Capabilities: {Capabilities}", string.Join(", ", context.Manifest.Capabilities));
                    _logger.LogInformation("  Load Time: {LoadTime}", context.LoadTime);
                }

                // Use the plugin's sensing capability
                var senseInstances = pluginHost.GetCapabilityInstances<ISense>();
                foreach (var senseInstance in senseInstances)
                {
                    var result = await senseInstance.SenseAsync("Hello from the host!");
                    _logger.LogInformation("Sensing result: {Result}", result);
                }

                // List all loaded plugins
                var loadedPlugins = pluginHost.GetLoadedPluginNames();
                _logger.LogInformation("Currently loaded plugins: {Plugins}", string.Join(", ", loadedPlugins));

                // Unload the plugin
                var unloadSuccess = await pluginHost.UnloadPluginAsync("TestPlugin");
                if (unloadSuccess)
                {
                    _logger.LogInformation("Plugin unloaded successfully!");
                }
                else
                {
                    _logger.LogError("Failed to unload plugin");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during plugin hosting demonstration");
            }
        }

        /// <summary>
        /// Demonstrates capability validation and security features.
        /// </summary>
        public async Task DemonstrateCapabilityValidation()
        {
            using var pluginHost = new PluginHost(_logger);

            try
            {
                // This would fail if the plugin doesn't implement declared capabilities
                var pluginPath = "test-plugins/TestPlugin/bin/Debug/net8.0/TestPlugin.dll";
                var loadSuccess = await pluginHost.LoadPluginAsync(pluginPath);

                if (loadSuccess)
                {
                    _logger.LogInformation("Plugin passed capability validation");

                    // Verify only declared capabilities are available
                    var senseInstances = pluginHost.GetCapabilityInstances<ISense>();
                    var decideInstances = pluginHost.GetCapabilityInstances<IDecide>();
                    var actInstances = pluginHost.GetCapabilityInstances<IAct>();
                    var guardInstances = pluginHost.GetCapabilityInstances<IGuard>();

                    _logger.LogInformation("Available capabilities:");
                    _logger.LogInformation("  ISense: {Count} instances", senseInstances.Count());
                    _logger.LogInformation("  IDecide: {Count} instances", decideInstances.Count());
                    _logger.LogInformation("  IAct: {Count} instances", actInstances.Count());
                    _logger.LogInformation("  IGuard: {Count} instances", guardInstances.Count());
                }
                else
                {
                    _logger.LogWarning("Plugin failed capability validation");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during capability validation demonstration");
            }
        }

        /// <summary>
        /// Demonstrates plugin lifecycle auditing.
        /// </summary>
        public async Task DemonstrateLifecycleAuditing()
        {
            using var pluginHost = new PluginHost(_logger);

            try
            {
                _logger.LogInformation("Starting plugin lifecycle demonstration...");

                // Load plugin (will generate audit logs)
                var pluginPath = "test-plugins/TestPlugin/bin/Debug/net8.0/TestPlugin.dll";
                await pluginHost.LoadPluginAsync(pluginPath);

                // Simulate some work
                await Task.Delay(1000);

                // Unload plugin (will generate audit logs)
                await pluginHost.UnloadPluginAsync("TestPlugin");

                _logger.LogInformation("Plugin lifecycle demonstration completed. Check logs for audit entries.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during lifecycle auditing demonstration");
            }
        }
    }
}
