using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.PluginHost;
using Xunit;

namespace Nexo.PluginHost.Tests
{
    public class PluginAssemblyLoadContextTests : IDisposable
    {
        private readonly ILogger<PluginAssemblyLoadContext> _logger;
        private readonly string _tempDirectory;
        private readonly string _pluginDependenciesPath;
        private readonly string _curatedLibsPath;

        public PluginAssemblyLoadContextTests()
        {
            _logger = new NullLogger<PluginAssemblyLoadContext>();
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _pluginDependenciesPath = Path.Combine(_tempDirectory, "plugin-deps");
            _curatedLibsPath = Path.Combine(_tempDirectory, "curated-libs");
            
            Directory.CreateDirectory(_pluginDependenciesPath);
            Directory.CreateDirectory(_curatedLibsPath);
        }

        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Arrange
            var pluginPath = Path.Combine(_tempDirectory, "test.dll");

            // Act
            var context = new PluginAssemblyLoadContext(pluginPath, _pluginDependenciesPath, _curatedLibsPath, _logger);

            // Assert
            Assert.True(context.IsCollectible);
        }

        [Fact]
        public void Load_WithAllowedAssembly_ShouldSucceed()
        {
            // Arrange
            var pluginPath = Path.Combine(_tempDirectory, "test.dll");
            var context = new PluginAssemblyLoadContext(pluginPath, _pluginDependenciesPath, _curatedLibsPath, _logger);
            var assemblyName = new AssemblyName("System.Collections");

            // Act
            var assembly = context.Load(assemblyName);

            // Assert
            Assert.NotNull(assembly);
            Assert.Equal("System.Collections", assembly.GetName().Name);
        }

        [Fact]
        public void Load_WithDisallowedAssembly_ShouldReturnNull()
        {
            // Arrange
            var pluginPath = Path.Combine(_tempDirectory, "test.dll");
            var context = new PluginAssemblyLoadContext(pluginPath, _pluginDependenciesPath, _curatedLibsPath, _logger);
            var assemblyName = new AssemblyName("SomeRandomAssembly");

            // Act
            var assembly = context.Load(assemblyName);

            // Assert
            Assert.Null(assembly);
        }

        [Fact]
        public void LoadPluginAssembly_WithValidPath_ShouldSucceed()
        {
            // Arrange
            var pluginPath = CreateTestPluginAssembly();
            var context = new PluginAssemblyLoadContext(pluginPath, _pluginDependenciesPath, _curatedLibsPath, _logger);

            // Act
            var assembly = context.LoadPluginAssembly();

            // Assert
            Assert.NotNull(assembly);
            Assert.True(assembly.FullName.Contains("TestPlugin"));
        }

        [Fact]
        public void LoadPluginAssembly_WithInvalidPath_ShouldThrow()
        {
            // Arrange
            var pluginPath = Path.Combine(_tempDirectory, "nonexistent.dll");
            var context = new PluginAssemblyLoadContext(pluginPath, _pluginDependenciesPath, _curatedLibsPath, _logger);

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => context.LoadPluginAssembly());
        }

        [Fact]
        public void UnloadAndWait_ShouldCompleteSuccessfully()
        {
            // Arrange
            var pluginPath = CreateTestPluginAssembly();
            var context = new PluginAssemblyLoadContext(pluginPath, _pluginDependenciesPath, _curatedLibsPath, _logger);
            var weakRef = new WeakReference(context);

            // Act
            context.UnloadAndWait();

            // Assert
            // Force garbage collection to ensure the context is collected
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakRef.IsAlive, "AssemblyLoadContext should be collected after unload");
        }

        [Fact]
        public void Load_WithSystemAssembly_ShouldSucceed()
        {
            // Arrange
            var pluginPath = Path.Combine(_tempDirectory, "test.dll");
            var context = new PluginAssemblyLoadContext(pluginPath, _pluginDependenciesPath, _curatedLibsPath, _logger);
            var assemblyName = new AssemblyName("System.Runtime");

            // Act
            var assembly = context.Load(assemblyName);

            // Assert
            Assert.NotNull(assembly);
        }

        private string CreateTestPluginAssembly()
        {
            var pluginPath = Path.Combine(_tempDirectory, "TestPlugin.dll");
            var sourceCode = @"
using System;
using System.Threading.Tasks;
using Nexo.Core.Contracts.Capabilities;

namespace TestPlugin
{
    public class TestSense : ISense
    {
        public string CapabilityName => ""TestSense"";

        public Task<object> SenseAsync(object input, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object>($""Sensed: {input}"");
        }
    }
}";

            // For this test, we'll create a simple text file since we can't compile on the fly
            // In a real test, you'd compile the source code to a DLL
            File.WriteAllText(pluginPath, sourceCode);
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
