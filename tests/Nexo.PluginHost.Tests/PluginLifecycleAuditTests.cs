using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.PluginHost;
using Xunit;

namespace Nexo.PluginHost.Tests
{
    public partial class PluginLifecycleAuditTests : IDisposable
    {
        private readonly TestLogger<PluginHost> _logger;
        private readonly string _tempDirectory;

        public PluginLifecycleAuditTests()
        {
            _logger = new TestLogger<PluginHost>();
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        [Fact]
        public async Task PluginLifecycle_ShouldLogAllEvents()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            var pluginPath = await CreateTestPluginAsync();

            // Act & Assert - Load
            var loadResult = await pluginHost.LoadPluginAsync(pluginPath);
            Assert.True(loadResult);

            // Verify load events were logged
            var loadLogs = _logger.LogEntries.Where(e => e.Contains("Loading plugin") || e.Contains("Successfully loaded plugin")).ToList();
            Assert.True(loadLogs.Count >= 2, "Should log plugin loading events");

            // Act & Assert - Unload
            var unloadResult = await pluginHost.UnloadPluginAsync("TestPlugin");
            Assert.True(unloadResult);

            // Verify unload events were logged
            var unloadLogs = _logger.LogEntries.Where(e => e.Contains("Unloading plugin") || e.Contains("Successfully unloaded plugin")).ToList();
            Assert.True(unloadLogs.Count >= 2, "Should log plugin unloading events");
        }

        [Fact]
        public async Task PluginLoadFailure_ShouldLogError()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.dll");

            // Act
            var result = await pluginHost.LoadPluginAsync(nonExistentPath);

            // Assert
            Assert.False(result);
            var errorLogs = _logger.LogEntries.Where(e => e.Contains("Plugin file not found")).ToList();
            Assert.Single(errorLogs);
        }

        [Fact]
        public async Task PluginCapabilityValidation_ShouldLogValidationResults()
        {
            // Arrange
            using var pluginHost = new PluginHost(_logger);
            var pluginPath = await CreateTestPluginAsync();

            // Act
            var result = await pluginHost.LoadPluginAsync(pluginPath);

            // Assert
            Assert.True(result);
            var validationLogs = _logger.LogEntries.Where(e => e.Contains("Validated capability implementation")).ToList();
            Assert.NotEmpty(validationLogs);
        }

        private async Task<string> CreateTestPluginAsync()
        {
            var manifest = new
            {
                Name = "TestPlugin",
                Version = "1.0.0",
                Description = "A test plugin for unit testing with sensing capability",
                Author = "Test Author",
                MinimalNexoVersion = "1.0.0",
                Capabilities = new[] { "ISense" }
            };

            var manifestPath = Path.Combine(_tempDirectory, "TestPlugin.json");
            await File.WriteAllTextAsync(manifestPath, System.Text.Json.JsonSerializer.Serialize(manifest));

            // Create a dummy DLL file for testing
            var pluginPath = Path.Combine(_tempDirectory, "TestPlugin.dll");
            await File.WriteAllBytesAsync(pluginPath, new byte[] { 0x4D, 0x5A }); // PE header

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

    /// <summary>
    /// Test logger that captures log entries for verification.
    /// </summary>
    public partial class TestLogger<T> : ILogger<T>
    {
        public List<string> LogEntries { get; } = new List<string>();

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            LogEntries.Add($"[{logLevel}] {message}");
        }
    }
}
