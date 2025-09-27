using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Orchestration;
using Xunit;

namespace Nexo.Infrastructure.Tests.ToolGeneration
{
    /// <summary>
    /// Tests for ToolGenerationOrchestrator
    /// </summary>
    public partial class ToolGenerationOrchestratorTests
    {
        private readonly Mock<IModelProvider> _mockAiProvider;
        private readonly Mock<ICodeGenerator> _mockCodeGenerator;
        private readonly Mock<ICompilationService> _mockCompiler;
        private readonly Mock<IPluginLoader> _mockPluginLoader;
        private readonly Mock<IToolRepository> _mockToolRepo;
        private readonly Mock<ILogger<ToolGenerationOrchestrator>> _mockLogger;
        private readonly ToolGenerationOrchestrator _orchestrator;

        public ToolGenerationOrchestratorTests()
        {
            _mockAiProvider = new Mock<IModelProvider>();
            _mockCodeGenerator = new Mock<ICodeGenerator>();
            _mockCompiler = new Mock<ICompilationService>();
            _mockPluginLoader = new Mock<IPluginLoader>();
            _mockToolRepo = new Mock<IToolRepository>();
            _mockLogger = new Mock<ILogger<ToolGenerationOrchestrator>>();

            _orchestrator = new ToolGenerationOrchestrator(
                _mockAiProvider.Object,
                _mockCodeGenerator.Object,
                _mockCompiler.Object,
                _mockPluginLoader.Object,
                _mockToolRepo.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GenerateToolAsync_ValidDescription_ReturnsGeneratedTool()
        {
            // Arrange
            var description = "Create a JSON formatter";
            var generatedCode = new GeneratedCode
            {
                SourceCode = "public partial class JsonFormatter : IPlugin { /* implementation */ }",
                ToolName = "JsonFormatter",
                Description = "Formats JSON data",
                IsWrappedInPlugin = true
            };

            var compilationResult = new CompilationResult
            {
                Success = true,
                Assembly = new byte[] { 1, 2, 3, 4 }
            };

            var pluginLoadResult = new PluginLoadResult
            {
                IsSuccess = true,
                Plugin = new MockPlugin("JsonFormatter", "1.0", "Formats JSON data")
            };

            var savedTool = new SavedTool
            {
                Id = Guid.NewGuid(),
                Name = "JsonFormatter",
                Description = "Formats JSON data"
            };

            _mockCodeGenerator
                .Setup(x => x.GenerateFromDescriptionAsync(description, It.IsAny<CancellationToken>()))
                .ReturnsAsync(generatedCode);

            _mockCompiler
                .Setup(x => x.CompileToAssemblyAsync(generatedCode.SourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(compilationResult);

            _mockPluginLoader
                .Setup(x => x.LoadPluginAsync(compilationResult.Assembly, generatedCode.ToolName))
                .ReturnsAsync(pluginLoadResult);

            _mockToolRepo
                .Setup(x => x.SaveToolAsync(pluginLoadResult.Plugin, compilationResult.Assembly, generatedCode.SourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(savedTool);

            // Act
            var result = await _orchestrator.GenerateToolAsync(description);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("JsonFormatter", result.Name);
            Assert.Equal("Formats JSON data", result.Description);
            Assert.True(result.IsReady);
            Assert.NotNull(result.Plugin);

            _mockCodeGenerator.Verify(x => x.GenerateFromDescriptionAsync(description, It.IsAny<CancellationToken>()), Times.Once);
            _mockCompiler.Verify(x => x.CompileToAssemblyAsync(generatedCode.SourceCode, It.IsAny<CancellationToken>()), Times.Once);
            _mockPluginLoader.Verify(x => x.LoadPluginAsync(compilationResult.Assembly, generatedCode.ToolName), Times.Once);
            _mockToolRepo.Verify(x => x.SaveToolAsync(pluginLoadResult.Plugin, compilationResult.Assembly, generatedCode.SourceCode, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GenerateToolAsync_CompilationFails_ThrowsException()
        {
            // Arrange
            var description = "Create a JSON formatter";
            var generatedCode = new GeneratedCode
            {
                SourceCode = "invalid c# code",
                ToolName = "JsonFormatter",
                Description = "Formats JSON data"
            };

            var compilationResult = new CompilationResult
            {
                Success = false,
                Errors = new[] { "Syntax error" }
            };

            _mockCodeGenerator
                .Setup(x => x.GenerateFromDescriptionAsync(description, It.IsAny<CancellationToken>()))
                .ReturnsAsync(generatedCode);

            _mockCompiler
                .Setup(x => x.CompileToAssemblyAsync(generatedCode.SourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(compilationResult);

            _mockCompiler
                .Setup(x => x.FixAndCompileAsync(generatedCode.SourceCode, compilationResult.Errors, It.IsAny<CancellationToken>()))
                .ReturnsAsync(compilationResult);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _orchestrator.GenerateToolAsync(description));
        }

        [Fact]
        public async Task GenerateToolAsync_PluginLoadFails_ThrowsException()
        {
            // Arrange
            var description = "Create a JSON formatter";
            var generatedCode = new GeneratedCode
            {
                SourceCode = "public partial class JsonFormatter : IPlugin { /* implementation */ }",
                ToolName = "JsonFormatter",
                Description = "Formats JSON data"
            };

            var compilationResult = new CompilationResult
            {
                Success = true,
                Assembly = new byte[] { 1, 2, 3, 4 }
            };

            var pluginLoadResult = new PluginLoadResult
            {
                IsSuccess = false,
                Errors = { new PluginLoadError { Message = "Plugin load failed" } }
            };

            _mockCodeGenerator
                .Setup(x => x.GenerateFromDescriptionAsync(description, It.IsAny<CancellationToken>()))
                .ReturnsAsync(generatedCode);

            _mockCompiler
                .Setup(x => x.CompileToAssemblyAsync(generatedCode.SourceCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(compilationResult);

            _mockPluginLoader
                .Setup(x => x.LoadPluginAsync(compilationResult.Assembly, generatedCode.ToolName))
                .ReturnsAsync(pluginLoadResult);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _orchestrator.GenerateToolAsync(description));
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
