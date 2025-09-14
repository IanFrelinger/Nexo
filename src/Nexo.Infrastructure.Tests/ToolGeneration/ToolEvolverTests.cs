using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Infrastructure.Evolution;
using Xunit;

namespace Nexo.Infrastructure.Tests.ToolGeneration
{
    /// <summary>
    /// Tests for ToolEvolver
    /// </summary>
    public class ToolEvolverTests
    {
        private readonly Mock<ICodeGenerator> _mockCodeGenerator;
        private readonly Mock<ICompilationService> _mockCompiler;
        private readonly Mock<IPluginLoader> _mockPluginLoader;
        private readonly Mock<IToolRepository> _mockToolRepo;
        private readonly Mock<ILogger<ToolEvolver>> _mockLogger;
        private readonly ToolEvolver _toolEvolver;

        public ToolEvolverTests()
        {
            _mockCodeGenerator = new Mock<ICodeGenerator>();
            _mockCompiler = new Mock<ICompilationService>();
            _mockPluginLoader = new Mock<IPluginLoader>();
            _mockToolRepo = new Mock<IToolRepository>();
            _mockLogger = new Mock<ILogger<ToolEvolver>>();

            _toolEvolver = new ToolEvolver(
                _mockCodeGenerator.Object,
                _mockCompiler.Object,
                _mockPluginLoader.Object,
                _mockToolRepo.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task EvolveToolAsync_ValidEvolution_ReturnsEvolvedTool()
        {
            // Arrange
            var toolName = "TestPlugin";
            var modification = "add XML formatting support";
            var existingCode = @"
public class TestPlugin : IPlugin
{
    public string Name => ""TestPlugin"";
    public string Version => ""1.0"";
    public string Description => ""Test plugin"";
    public string Author => ""Test"";
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
        return Task.FromResult(new PluginResult { Success = true, Message = ""Test executed"" });
    }
}";

            var evolvedCode = new GeneratedCode
            {
                SourceCode = existingCode + @"
    // Added XML formatting support
    public string FormatAsXml(string data) { return ""<data>"" + data + ""</data>""; }",
                ToolName = toolName,
                Description = "Test plugin with XML support"
            };

            var compilationResult = new CompilationResult
            {
                Success = true,
                Assembly = new byte[] { 1, 2, 3, 4, 5 }
            };

            var pluginLoadResult = new PluginLoadResult
            {
                IsSuccess = true,
                Plugin = new MockPlugin(toolName, "2.0", "Test plugin with XML support")
            };

            var currentTool = new ToolInfo
            {
                Name = toolName,
                Version = 1,
                Description = "Test plugin"
            };

            _mockToolRepo
                .Setup(x => x.LoadToolSourceAsync(toolName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCode);

            _mockCodeGenerator
                .Setup(x => x.GenerateFromPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(evolvedCode);

            _mockCompiler
                .Setup(x => x.CompileToAssemblyAsync(evolvedCode.SourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(compilationResult);

            _mockPluginLoader
                .Setup(x => x.LoadPluginAsync(compilationResult.Assembly, toolName))
                .ReturnsAsync(pluginLoadResult);

            _mockToolRepo
                .Setup(x => x.GetToolAsync(toolName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentTool);

            _mockToolRepo
                .Setup(x => x.SaveVersionAsync(toolName, evolvedCode.SourceCode, 2, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _toolEvolver.EvolveToolAsync(toolName, modification);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(toolName, result.Name);
            Assert.Equal(2, result.Version);
            Assert.Equal(modification, result.EvolutionDescription);
            Assert.True(result.Success);
            Assert.Empty(result.Errors);

            _mockToolRepo.Verify(x => x.LoadToolSourceAsync(toolName, It.IsAny<CancellationToken>()), Times.Once);
            _mockCodeGenerator.Verify(x => x.GenerateFromPromptAsync(It.Is<string>(p => p.Contains(modification)), It.IsAny<CancellationToken>()), Times.Once);
            _mockCompiler.Verify(x => x.CompileToAssemblyAsync(evolvedCode.SourceCode, It.IsAny<CancellationToken>()), Times.Once);
            _mockPluginLoader.Verify(x => x.LoadPluginAsync(compilationResult.Assembly, toolName), Times.Once);
            _mockToolRepo.Verify(x => x.SaveVersionAsync(toolName, evolvedCode.SourceCode, 2, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EvolveToolAsync_ToolNotFound_ReturnsFailure()
        {
            // Arrange
            var toolName = "NonExistentTool";
            var modification = "add new feature";

            _mockToolRepo
                .Setup(x => x.LoadToolSourceAsync(toolName, It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _toolEvolver.EvolveToolAsync(toolName, modification);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(toolName, result.Name);
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("Tool source not found", result.Errors[0]);
        }

        [Fact]
        public async Task EvolveToolAsync_CompilationFails_ReturnsFailure()
        {
            // Arrange
            var toolName = "TestPlugin";
            var modification = "add new feature";
            var existingCode = "public class TestPlugin : IPlugin { }";

            var evolvedCode = new GeneratedCode
            {
                SourceCode = "invalid c# code",
                ToolName = toolName,
                Description = "Test plugin"
            };

            var compilationResult = new CompilationResult
            {
                Success = false,
                Errors = new[] { "Syntax error" }
            };

            _mockToolRepo
                .Setup(x => x.LoadToolSourceAsync(toolName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCode);

            _mockCodeGenerator
                .Setup(x => x.GenerateFromPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(evolvedCode);

            _mockCompiler
                .Setup(x => x.CompileToAssemblyAsync(evolvedCode.SourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(compilationResult);

            _mockCompiler
                .Setup(x => x.FixAndCompileAsync(evolvedCode.SourceCode, compilationResult.Errors, It.IsAny<CancellationToken>()))
                .ReturnsAsync(compilationResult);

            // Act
            var result = await _toolEvolver.EvolveToolAsync(toolName, modification);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(toolName, result.Name);
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task EvolveToolAsync_PluginLoadFails_ReturnsFailure()
        {
            // Arrange
            var toolName = "TestPlugin";
            var modification = "add new feature";
            var existingCode = "public class TestPlugin : IPlugin { }";

            var evolvedCode = new GeneratedCode
            {
                SourceCode = "public class TestPlugin : IPlugin { /* valid code */ }",
                ToolName = toolName,
                Description = "Test plugin"
            };

            var compilationResult = new CompilationResult
            {
                Success = true,
                Assembly = new byte[] { 1, 2, 3, 4, 5 }
            };

            var pluginLoadResult = new PluginLoadResult
            {
                IsSuccess = false,
                Errors = { new PluginLoadError { Message = "Plugin load failed" } }
            };

            _mockToolRepo
                .Setup(x => x.LoadToolSourceAsync(toolName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCode);

            _mockCodeGenerator
                .Setup(x => x.GenerateFromPromptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(evolvedCode);

            _mockCompiler
                .Setup(x => x.CompileToAssemblyAsync(evolvedCode.SourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(compilationResult);

            _mockPluginLoader
                .Setup(x => x.LoadPluginAsync(compilationResult.Assembly, toolName))
                .ReturnsAsync(pluginLoadResult);

            // Act
            var result = await _toolEvolver.EvolveToolAsync(toolName, modification);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(toolName, result.Name);
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("Failed to load evolved plugin", result.Errors[0]);
        }

        [Fact]
        public async Task CanEvolveToolAsync_ExistingTool_ReturnsTrue()
        {
            // Arrange
            var toolName = "TestPlugin";
            var toolInfo = new ToolInfo
            {
                Name = toolName,
                Version = 1,
                Description = "Test plugin"
            };

            _mockToolRepo
                .Setup(x => x.GetToolAsync(toolName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(toolInfo);

            // Act
            var result = await _toolEvolver.CanEvolveToolAsync(toolName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanEvolveToolAsync_NonExistentTool_ReturnsFalse()
        {
            // Arrange
            var toolName = "NonExistentTool";

            _mockToolRepo
                .Setup(x => x.GetToolAsync(toolName, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ToolInfo?)null);

            // Act
            var result = await _toolEvolver.CanEvolveToolAsync(toolName);

            // Assert
            Assert.False(result);
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
