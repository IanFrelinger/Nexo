using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.PluginHost;
using Xunit;

namespace Nexo.PluginHost.Tests
{
    public partial class PluginHostOpenTelemetryTests : IDisposable
    {
        private readonly ILogger<PluginHost> _logger;
        private readonly string _tempDirectory;
        private readonly List<Activity> _activities;

        public PluginHostOpenTelemetryTests()
        {
            _logger = new NullLogger<PluginHost>();
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _activities = new List<Activity>();
            
            Directory.CreateDirectory(_tempDirectory);
            
            // Set up ActivityListener to capture activities
            var activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "Nexo.PluginHost",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => _activities.Add(activity),
                ActivityStopped = activity => { }
            };
            
            ActivitySource.AddActivityListener(activityListener);
        }

        [Fact]
        public async Task LoadPlugin_ShouldEmitOpenTelemetrySpans()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            var pluginPath = await CreateTestPluginAsync();

            // Act
            var result = await pluginHost.LoadPluginAsync(pluginPath);

            // Assert
            Assert.True(result);
            
            // Verify that activities were created
            var loadActivities = _activities.Where(a => a.OperationName == "LoadPlugin").ToList();
            Assert.Single(loadActivities);
            
            var loadActivity = loadActivities.First();
            Assert.Equal("LoadPlugin", loadActivity.OperationName);
            Assert.Contains(loadActivity.Tags, t => t.Key == "plugin.path" && t.Value == pluginPath);
            Assert.Contains(loadActivity.Tags, t => t.Key == "plugin.name" && t.Value == "TestPlugin");
            Assert.Contains(loadActivity.Tags, t => t.Key == "plugin.version" && t.Value == "1.0.0");
            Assert.Contains(loadActivity.Tags, t => t.Key == "plugin.capabilities");
            Assert.Contains(loadActivity.Tags, t => t.Key == "load.duration.ms");
            Assert.Contains(loadActivity.Tags, t => t.Key == "capability.count");
        }

        [Fact]
        public async Task UnloadPlugin_ShouldEmitOpenTelemetrySpans()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            var pluginPath = await CreateTestPluginAsync();
            await pluginHost.LoadPluginAsync(pluginPath);

            // Act
            var result = await pluginHost.UnloadPluginAsync("TestPlugin");

            // Assert
            Assert.True(result);
            
            // Verify that unload activities were created
            var unloadActivities = _activities.Where(a => a.OperationName == "UnloadPlugin").ToList();
            Assert.Single(unloadActivities);
            
            var unloadActivity = unloadActivities.First();
            Assert.Equal("UnloadPlugin", unloadActivity.OperationName);
            Assert.Contains(unloadActivity.Tags, t => t.Key == "plugin.name" && t.Value == "TestPlugin");
            Assert.Contains(unloadActivity.Tags, t => t.Key == "unload.duration.ms");
        }

        [Fact]
        public async Task LoadPlugin_WithError_ShouldEmitErrorStatus()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            var invalidPluginPath = Path.Combine(_tempDirectory, "nonexistent.dll");

            // Act
            var result = await pluginHost.LoadPluginAsync(invalidPluginPath);

            // Assert
            Assert.False(result);
            
            // Verify that error activities were created
            var loadActivities = _activities.Where(a => a.OperationName == "LoadPlugin").ToList();
            Assert.Single(loadActivities);
            
            var loadActivity = loadActivities.First();
            Assert.Equal(ActivityStatusCode.Error, loadActivity.Status);
            Assert.Contains(loadActivity.Tags, t => t.Key == "plugin.path" && t.Value == invalidPluginPath);
        }

        [Fact]
        public async Task PluginLifecycle_ShouldEmitCompleteTrace()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            var pluginPath = await CreateTestPluginAsync();

            // Act
            var loadResult = await pluginHost.LoadPluginAsync(pluginPath);
            var unloadResult = await pluginHost.UnloadPluginAsync("TestPlugin");

            // Assert
            Assert.True(loadResult);
            Assert.True(unloadResult);
            
            // Verify complete trace
            var allActivities = _activities.ToList();
            Assert.Equal(2, allActivities.Count);
            
            var loadActivity = allActivities.First(a => a.OperationName == "LoadPlugin");
            var unloadActivity = allActivities.First(a => a.OperationName == "UnloadPlugin");
            
            Assert.Equal(ActivityStatusCode.Ok, loadActivity.Status);
            Assert.Equal(ActivityStatusCode.Ok, unloadActivity.Status);
            
            // Verify timing
            Assert.True(loadActivity.StartTimeUtc < unloadActivity.StartTimeUtc);
        }

        [Fact]
        public async Task MultiplePlugins_ShouldEmitSeparateSpans()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            var plugin1Path = await CreateTestPluginAsync("Plugin1", "1.0.0");
            var plugin2Path = await CreateTestPluginAsync("Plugin2", "2.0.0");

            // Act
            var load1Result = await pluginHost.LoadPluginAsync(plugin1Path);
            var load2Result = await pluginHost.LoadPluginAsync(plugin2Path);
            var unload1Result = await pluginHost.UnloadPluginAsync("Plugin1");
            var unload2Result = await pluginHost.UnloadPluginAsync("Plugin2");

            // Assert
            Assert.True(load1Result);
            Assert.True(load2Result);
            Assert.True(unload1Result);
            Assert.True(unload2Result);
            
            // Verify separate spans for each plugin
            var loadActivities = _activities.Where(a => a.OperationName == "LoadPlugin").ToList();
            var unloadActivities = _activities.Where(a => a.OperationName == "UnloadPlugin").ToList();
            
            Assert.Equal(2, loadActivities.Count);
            Assert.Equal(2, unloadActivities.Count);
            
            var plugin1Load = loadActivities.First(a => a.Tags.Any(t => t.Key == "plugin.name" && t.Value == "Plugin1"));
            var plugin2Load = loadActivities.First(a => a.Tags.Any(t => t.Key == "plugin.name" && t.Value == "Plugin2"));
            var plugin1Unload = unloadActivities.First(a => a.Tags.Any(t => t.Key == "plugin.name" && t.Value == "Plugin1"));
            var plugin2Unload = unloadActivities.First(a => a.Tags.Any(t => t.Key == "plugin.name" && t.Value == "Plugin2"));
            
            Assert.NotNull(plugin1Load);
            Assert.NotNull(plugin2Load);
            Assert.NotNull(plugin1Unload);
            Assert.NotNull(plugin2Unload);
        }

        private async Task<string> CreateTestPluginAsync(string pluginName = "TestPlugin", string version = "1.0.0")
        {
            var pluginPath = Path.Combine(_tempDirectory, $"{pluginName}.dll");
            var manifestPath = Path.Combine(_tempDirectory, $"{pluginName}.json");
            
            // Create a simple plugin manifest
            var manifest = new
            {
                Name = pluginName,
                Version = version,
                Description = $"Test plugin {pluginName}",
                Author = "Test Author",
                MinimalNexoVersion = "1.0.0",
                Capabilities = new[] { "ISense" }
            };
            
            var manifestJson = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(manifestPath, manifestJson);
            
            // Create a placeholder DLL file (in real tests, this would be a compiled assembly)
            await File.WriteAllTextAsync(pluginPath, "placeholder");
            
            return pluginPath;
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
    }
}
