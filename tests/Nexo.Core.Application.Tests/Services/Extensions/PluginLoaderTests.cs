using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Application.Services.Extensions;
using Nexo.Core.Domain.Models.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Core.Application.Tests.Services.Extensions
{
    public class PluginLoaderTests
    {
        private readonly Mock<ILogger<PluginLoader>> _mockLogger;
        private readonly PluginLoader _pluginLoader;

        public PluginLoaderTests()
        {
            _mockLogger = new Mock<ILogger<PluginLoader>>();
            _pluginLoader = new PluginLoader(_mockLogger.Object);
        }

        [Fact]
        public async Task LoadPluginAsync_WithValidAssembly_ShouldReturnSuccess()
        {
            // Arrange
            var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestPlugins", "bin", "Debug", "net8.0", "TestPlugin.dll");
            var pluginName = "TestPlugin";

            // Act
            var result = await _pluginLoader.LoadPluginAsync(assemblyPath, pluginName);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Plugin);
            Assert.Equal(pluginName, result.PluginName);
            Assert.False(result.HasErrors);
            Assert.True(result.LoadTime > TimeSpan.Zero);
        }

        [Fact]
        public async Task LoadPluginAsync_WithEmptyAssembly_ShouldReturnFailure()
        {
            // Arrange
            var assemblyBytes = new byte[0];
            var pluginName = "TestPlugin";

            // Act
            var result = await _pluginLoader.LoadPluginAsync(assemblyBytes, pluginName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(result.Plugin);
            Assert.True(result.HasErrors);
            Assert.Contains(result.Errors, e => e.Message.Contains("Assembly bytes cannot be empty"));
        }

        [Fact]
        public async Task LoadPluginAsync_WithEmptyPluginName_ShouldReturnFailure()
        {
            // Arrange
            var assemblyBytes = new byte[] { 0x4D, 0x5A };
            var pluginName = "";

            // Act
            var result = await _pluginLoader.LoadPluginAsync(assemblyBytes, pluginName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(result.Plugin);
            Assert.True(result.HasErrors);
            Assert.Contains(result.Errors, e => e.Message.Contains("Plugin name cannot be empty"));
        }

        [Fact]
        public async Task LoadPluginAsync_WithInvalidAssembly_ShouldReturnFailure()
        {
            // Arrange
            var assemblyBytes = new byte[] { 0x00, 0x01, 0x02 }; // Invalid assembly
            var pluginName = "TestPlugin";

            // Act
            var result = await _pluginLoader.LoadPluginAsync(assemblyBytes, pluginName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(result.Plugin);
            Assert.True(result.HasErrors);
        }

        [Fact]
        public async Task UnloadPluginAsync_WithLoadedPlugin_ShouldReturnTrue()
        {
            // Arrange
            var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestPlugins", "bin", "Debug", "net8.0", "TestPlugin.dll");
            var pluginName = "TestPlugin";
            await _pluginLoader.LoadPluginAsync(assemblyPath, pluginName);

            // Act
            var result = await _pluginLoader.UnloadPluginAsync(pluginName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UnloadPluginAsync_WithNonExistentPlugin_ShouldReturnFalse()
        {
            // Arrange
            var pluginName = "NonExistentPlugin";

            // Act
            var result = await _pluginLoader.UnloadPluginAsync(pluginName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetPluginAsync_WithLoadedPlugin_ShouldReturnPlugin()
        {
            // Arrange
            var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestPlugins", "bin", "Debug", "net8.0", "TestPlugin.dll");
            var pluginName = "TestPlugin";
            await _pluginLoader.LoadPluginAsync(assemblyPath, pluginName);

            // Act
            var plugin = await _pluginLoader.GetPluginAsync(pluginName);

            // Assert
            Assert.NotNull(plugin);
        }

        [Fact]
        public async Task GetPluginAsync_WithNonExistentPlugin_ShouldReturnNull()
        {
            // Arrange
            var pluginName = "NonExistentPlugin";

            // Act
            var plugin = await _pluginLoader.GetPluginAsync(pluginName);

            // Assert
            Assert.Null(plugin);
        }

        [Fact]
        public async Task IsPluginLoadedAsync_WithLoadedPlugin_ShouldReturnTrue()
        {
            // Arrange
            var assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestPlugins", "bin", "Debug", "net8.0", "TestPlugin.dll");
            var pluginName = "TestPlugin";
            await _pluginLoader.LoadPluginAsync(assemblyPath, pluginName);

            // Act
            var isLoaded = await _pluginLoader.IsPluginLoadedAsync(pluginName);

            // Assert
            Assert.True(isLoaded);
        }

        [Fact]
        public async Task IsPluginLoadedAsync_WithNonExistentPlugin_ShouldReturnFalse()
        {
            // Arrange
            var pluginName = "NonExistentPlugin";

            // Act
            var isLoaded = await _pluginLoader.IsPluginLoadedAsync(pluginName);

            // Assert
            Assert.False(isLoaded);
        }
    }
}
