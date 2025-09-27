using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Infrastructure.Persistence;
using Xunit;

namespace Nexo.Infrastructure.Tests.ToolGeneration
{
    /// <summary>
    /// Tests for ToolRepository
    /// </summary>
    public partial class ToolRepositoryTests : IDisposable
    {
        private readonly Mock<ILogger<ToolRepository>> _mockLogger;
        private readonly string _testToolsPath;
        private readonly ToolRepository _toolRepository;

        public ToolRepositoryTests()
        {
            _mockLogger = new Mock<ILogger<ToolRepository>>();
            _testToolsPath = Path.Combine(Path.GetTempPath(), "nexo_test_tools", Guid.NewGuid().ToString());
            _toolRepository = new ToolRepository(_mockLogger.Object);
        }

        [Fact]
        public async Task SaveToolAsync_ValidPlugin_SavesSuccessfully()
        {
            // Arrange
            var plugin = new MockPlugin("TestPlugin", "1.0", "Test plugin");
            var assembly = new byte[] { 1, 2, 3, 4, 5 };
            var sourceCode = "public partial class TestPlugin : IPlugin { /* implementation */ }";

            // Act
            var result = await _toolRepository.SaveToolAsync(plugin, assembly, sourceCode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestPlugin", result.Name);
            Assert.Equal("Test plugin", result.Description);
            Assert.Equal(1, result.Version);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.True(File.Exists(result.AssemblyPath));
            Assert.True(File.Exists(result.SourcePath));

            // Verify assembly content
            var savedAssembly = await File.ReadAllBytesAsync(result.AssemblyPath);
            Assert.Equal(assembly, savedAssembly);

            // Verify source content
            var savedSource = await File.ReadAllTextAsync(result.SourcePath);
            Assert.Equal(sourceCode, savedSource);
        }

        [Fact]
        public async Task ListToolsAsync_NoTools_ReturnsEmpty()
        {
            // Act
            var result = await _toolRepository.ListToolsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task ListToolsAsync_WithTools_ReturnsTools()
        {
            // Arrange
            var plugin1 = new MockPlugin("Plugin1", "1.0", "First plugin");
            var plugin2 = new MockPlugin("Plugin2", "1.0", "Second plugin");
            var assembly = new byte[] { 1, 2, 3, 4, 5 };
            var sourceCode = "public partial class TestPlugin : IPlugin { /* implementation */ }";

            await _toolRepository.SaveToolAsync(plugin1, assembly, sourceCode);
            await _toolRepository.SaveToolAsync(plugin2, assembly, sourceCode);

            // Act
            var result = await _toolRepository.ListToolsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            
            var toolNames = result.Select(t => t.Name).ToList();
            Assert.Contains("Plugin1", toolNames);
            Assert.Contains("Plugin2", toolNames);
        }

        [Fact]
        public async Task GetToolAsync_ExistingTool_ReturnsTool()
        {
            // Arrange
            var plugin = new MockPlugin("TestPlugin", "1.0", "Test plugin");
            var assembly = new byte[] { 1, 2, 3, 4, 5 };
            var sourceCode = "public partial class TestPlugin : IPlugin { /* implementation */ }";

            await _toolRepository.SaveToolAsync(plugin, assembly, sourceCode);

            // Act
            var result = await _toolRepository.GetToolAsync("TestPlugin");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestPlugin", result.Name);
            Assert.Equal("Test plugin", result.Description);
        }

        [Fact]
        public async Task GetToolAsync_NonExistentTool_ReturnsNull()
        {
            // Act
            var result = await _toolRepository.GetToolAsync("NonExistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoadToolSourceAsync_ExistingTool_ReturnsSource()
        {
            // Arrange
            var plugin = new MockPlugin("TestPlugin", "1.0", "Test plugin");
            var assembly = new byte[] { 1, 2, 3, 4, 5 };
            var sourceCode = "public partial class TestPlugin : IPlugin { /* implementation */ }";

            await _toolRepository.SaveToolAsync(plugin, assembly, sourceCode);

            // Act
            var result = await _toolRepository.LoadToolSourceAsync("TestPlugin");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sourceCode, result);
        }

        [Fact]
        public async Task LoadToolSourceAsync_NonExistentTool_ReturnsNull()
        {
            // Act
            var result = await _toolRepository.LoadToolSourceAsync("NonExistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SaveVersionAsync_ExistingTool_SavesNewVersion()
        {
            // Arrange
            var plugin = new MockPlugin("TestPlugin", "1.0", "Test plugin");
            var assembly = new byte[] { 1, 2, 3, 4, 5 };
            var sourceCode = "public partial class TestPlugin : IPlugin { /* implementation */ }";
            var newSourceCode = "public partial class TestPlugin : IPlugin { /* updated implementation */ }";

            await _toolRepository.SaveToolAsync(plugin, assembly, sourceCode);

            // Act
            await _toolRepository.SaveVersionAsync("TestPlugin", newSourceCode, 2);

            // Assert
            var updatedSource = await _toolRepository.LoadToolSourceAsync("TestPlugin");
            Assert.Equal(newSourceCode, updatedSource);
        }

        [Fact]
        public async Task SaveVersionAsync_NonExistentTool_ThrowsException()
        {
            // Arrange
            var newSourceCode = "public partial class TestPlugin : IPlugin { /* updated implementation */ }";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _toolRepository.SaveVersionAsync("NonExistent", newSourceCode, 2));
        }

        public void Dispose()
        {
            // Clean up test directory
            if (Directory.Exists(_testToolsPath))
            {
                Directory.Delete(_testToolsPath, true);
            }
        }

        // Mock plugin implementation for testing
        private class MockPlugin : IPlugin
        {
            public string Name { get; }
            public string Version { get; }
            public string Description { get; }
            public string Author => "Test";
            public bool IsEnabled => true;

            public MockPlugin(string name, string version, string description)
            {
                Name = name;
                Version = version;
                Description = description;
            }

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
                return Task.FromResult(new PluginResult
                {
                    Success = true,
                    Message = "Mock plugin executed"
                });
            }
        }
    }
}
