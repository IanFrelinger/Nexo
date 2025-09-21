using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.PluginHost;
using Xunit;

namespace Nexo.PluginHost.Tests
{
    public class PluginHostTests : IDisposable
    {
        private readonly ILogger<PluginHost> _logger;
        private readonly string _testPluginPath;
        private readonly string _testPluginManifestPath;
        private readonly string _tempDirectory;

        public PluginHostTests()
        {
            _logger = new NullLogger<PluginHost>();
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            // Build the test plugin
            _testPluginPath = BuildTestPlugin();
            _testPluginManifestPath = Path.ChangeExtension(_testPluginPath, ".json");
        }

        [Fact]
        public async Task LoadPlugin_WithValidManifest_ShouldSucceed()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);

            // Act
            var result = await pluginHost.LoadPluginAsync(_testPluginPath);

            // Assert
            Assert.True(result);
            Assert.Single(pluginHost.GetLoadedPluginNames());
            Assert.Equal("TestPlugin", pluginHost.GetLoadedPluginNames().First());
        }

        [Fact]
        public async Task LoadPlugin_WithInvalidManifest_ShouldFail()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            var invalidManifestPath = Path.Combine(_tempDirectory, "invalid.json");
            await File.WriteAllTextAsync(invalidManifestPath, "invalid json");

            // Act
            var result = await pluginHost.LoadPluginAsync(invalidManifestPath);

            // Assert
            Assert.False(result);
            Assert.Empty(pluginHost.GetLoadedPluginNames());
        }

        [Fact]
        public async Task LoadPlugin_WithNoCapabilities_ShouldFail()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            var manifestPath = Path.Combine(_tempDirectory, "nocapabilities.json");
            var manifest = new
            {
                Name = "NoCapabilitiesPlugin",
                Version = "1.0.0",
                Capabilities = new string[0],
                MinimalNexoVersion = "1.0.0"
            };
            await File.WriteAllTextAsync(manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest));

            // Act
            var result = await pluginHost.LoadPluginAsync(manifestPath);

            // Assert
            Assert.False(result);
            Assert.Empty(pluginHost.GetLoadedPluginNames());
        }

        [Fact]
        public async Task UnloadPlugin_ShouldSucceed()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            await pluginHost.LoadPluginAsync(_testPluginPath);

            // Act
            var result = await pluginHost.UnloadPluginAsync("TestPlugin");

            // Assert
            Assert.True(result);
            Assert.Empty(pluginHost.GetLoadedPluginNames());
        }

        [Fact]
        public async Task UnloadPlugin_NonExistentPlugin_ShouldReturnFalse()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);

            // Act
            var result = await pluginHost.UnloadPluginAsync("NonExistentPlugin");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetCapabilityInstances_ShouldReturnCorrectInstances()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            await pluginHost.LoadPluginAsync(_testPluginPath);

            // Act
            var senseInstances = pluginHost.GetCapabilityInstances<ISense>().ToList();

            // Assert
            Assert.Single(senseInstances);
            Assert.Equal("TestSense", senseInstances.First().CapabilityName);
        }

        [Fact]
        public async Task PluginContext_ShouldContainCorrectInformation()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            await pluginHost.LoadPluginAsync(_testPluginPath);

            // Act
            var context = pluginHost.GetPluginContext("TestPlugin");

            // Assert
            Assert.NotNull(context);
            Assert.Equal("TestPlugin", context.Manifest.Name);
            Assert.Equal("1.0.0", context.Manifest.Version);
            Assert.Single(context.Instances);
            Assert.True(context.LoadTime <= DateTime.UtcNow);
        }

        [Fact]
        public async Task AssemblyLoadContext_ShouldBeCollectible()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            await pluginHost.LoadPluginAsync(_testPluginPath);

            var context = pluginHost.GetPluginContext("TestPlugin");
            var weakRef = new WeakReference(context.LoadContext);

            // Act
            await pluginHost.UnloadPluginAsync("TestPlugin");

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Assert
            Assert.False(weakRef.IsAlive, "AssemblyLoadContext should be collected after unload");
        }

        [Fact]
        public async Task LoadPlugin_WithCapabilityMismatch_ShouldFail()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            var manifestPath = Path.Combine(_tempDirectory, "mismatch.json");
            var manifest = new
            {
                Name = "MismatchPlugin",
                Version = "1.0.0",
                Capabilities = new[] { "IDecide" }, // Declares IDecide but implements ISense
                MinimalNexoVersion = "1.0.0"
            };
            await File.WriteAllTextAsync(manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest));

            // Act
            var result = await pluginHost.LoadPluginAsync(manifestPath);

            // Assert
            Assert.False(result);
            Assert.Empty(pluginHost.GetLoadedPluginNames());
        }

        [Fact]
        public async Task Dispose_ShouldUnloadAllPlugins()
        {
            // Arrange
            var pluginHost = new PluginHost(_logger);
            await pluginHost.LoadPluginAsync(_testPluginPath);
            Assert.Single(pluginHost.GetLoadedPluginNames());

            // Act
            pluginHost.Dispose();

            // Assert
            Assert.Empty(pluginHost.GetLoadedPluginNames());
        }

        private string BuildTestPlugin()
        {
            var testPluginSource = @"
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Contracts.Capabilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestPlugin
{
    public class TestPlugin : IPlugin
    {
        public string Name => ""TestPlugin"";
        public string Version => ""1.0.0"";
        public string Description => ""A test plugin for unit testing"";
        public string Author => ""Test Author"";
        public bool IsEnabled => true;

        public Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<PluginResult> ExecuteAsync(string[] args)
        {
            return Task.FromResult(new PluginResult { Success = true, Message = ""TestPlugin executed successfully"" });
        }
    }

    public class TestSenseCapability : ISense
    {
        public string CapabilityName => ""TestSense"";

        public Task<object> SenseAsync(object input, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object>($""Sensed: {input}"");
        }
    }
}";

            var manifest = @"
{
  ""Name"": ""TestPlugin"",
  ""Version"": ""1.0.0"",
  ""Description"": ""A test plugin for unit testing with sensing capability"",
  ""Author"": ""Test Author"",
  ""MinimalNexoVersion"": ""1.0.0"",
  ""Capabilities"": [""ISense""]
}";

            var pluginPath = Path.Combine(_tempDirectory, "TestPlugin.dll");
            var manifestPath = Path.Combine(_tempDirectory, "TestPlugin.json");

            // Write source files
            var sourcePath = Path.Combine(_tempDirectory, "TestPlugin.cs");
            await File.WriteAllTextAsync(sourcePath, testPluginSource);
            await File.WriteAllTextAsync(manifestPath, manifest);

            // Note: In a real test environment, you would compile the plugin here
            // For this example, we'll create a placeholder that would be compiled
            // in a real implementation

            return pluginPath;
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
    }
}
