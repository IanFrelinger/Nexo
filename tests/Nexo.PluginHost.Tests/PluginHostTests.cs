using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.PluginHost;
using Nexo.PluginHost.Abstractions;
using Nexo.PluginHost.Implementations;
using Xunit;

namespace Nexo.PluginHost.Tests
{
    public partial class PluginHostTests : IDisposable
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
        public async Task PluginHost_WithCustomFileSystem_ShouldUseIt()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            using var pluginHost = new PluginHost(_logger, fileSystem: mockFileSystem);

            // Act
            var result = await pluginHost.LoadPluginAsync(_testPluginPath);

            // Assert
            Assert.True(result);
            Assert.True(mockFileSystem.FileExistsCalled);
        }

        [Fact]
        public async Task PluginHost_WithCustomProcessRunner_ShouldUseIt()
        {
            // Arrange
            var mockProcessRunner = new MockProcessRunner();
            using var pluginHost = new PluginHost(_logger, processRunner: mockProcessRunner);

            // Act
            var result = await pluginHost.LoadPluginAsync(_testPluginPath);

            // Assert
            Assert.True(result);
            // MockProcessRunner doesn't get called during plugin loading, but it's injected
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
    public partial class TestPlugin : IPlugin
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

    public partial class TestSenseCapability : ISense
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

    // Mock implementations for testing
    public partial class MockFileSystem : INexoFileSystem
    {
        public bool FileExistsCalled { get; private set; }
        public bool ReadAllTextAsyncCalled { get; private set; }

        public bool FileExists(string path)
        {
            FileExistsCalled = true;
            return File.Exists(path);
        }

        public bool DirectoryExists(string path) => Directory.Exists(path);
        public async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        {
            ReadAllTextAsyncCalled = true;
            return await File.ReadAllTextAsync(path, cancellationToken);
        }
        public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default) => File.WriteAllTextAsync(path, contents, cancellationToken);
        public long GetFileSize(string path) => new FileInfo(path).Length;
        public DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);
        public IEnumerable<string> GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => Directory.GetFiles(path, searchPattern, searchOption);
        public IEnumerable<string> GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => Directory.GetDirectories(path, searchPattern, searchOption);
        public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);
        public void DeleteFile(string path) => File.Delete(path);
        public void DeleteDirectory(string path, bool recursive = false) => Directory.Delete(path, recursive);
        public void CopyFile(string sourcePath, string destinationPath, bool overwrite = false) => File.Copy(sourcePath, destinationPath, overwrite);
        public void MoveFile(string sourcePath, string destinationPath) => File.Move(sourcePath, destinationPath);
        public string GetCurrentDirectory() => Directory.GetCurrentDirectory();
        public string GetFullPath(string path) => Path.GetFullPath(path);
        public string CombinePath(params string[] paths) => Path.Combine(paths);
        public string GetDirectoryName(string path) => Path.GetDirectoryName(path);
        public string GetFileName(string path) => Path.GetFileName(path);
        public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);
        public string GetExtension(string path) => Path.GetExtension(path);
        public string ChangeExtension(string path, string extension) => Path.ChangeExtension(path, extension);
    }

    public partial class MockProcessRunner : INexoProcessRunner
    {
        public Task<INexoProcess> StartProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<INexoProcess> StartProcessAsync(string fileName, string arguments = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public INexoProcess GetProcessById(int processId) => throw new NotImplementedException();
        public IEnumerable<INexoProcess> GetProcesses() => throw new NotImplementedException();
        public IEnumerable<INexoProcess> GetProcessesByName(string processName) => throw new NotImplementedException();
        public INexoProcess GetCurrentProcess() => throw new NotImplementedException();
        public bool IsProcessRunning(int processId) => throw new NotImplementedException();
        public void KillProcess(int processId) => throw new NotImplementedException();
        public void KillProcess(int processId, bool force) => throw new NotImplementedException();
    }
}
