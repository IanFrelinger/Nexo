using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Compilation;
using Xunit;

namespace Nexo.Infrastructure.Tests.ToolGeneration
{
    /// <summary>
    /// Tests for RoslynCompilationService
    /// </summary>
    public partial class RoslynCompilationServiceTests
    {
        private readonly Mock<IModelProvider> _mockAiProvider;
        private readonly Mock<ILogger<RoslynCompilationService>> _mockLogger;
        private readonly RoslynCompilationService _compilationService;

        public RoslynCompilationServiceTests()
        {
            _mockAiProvider = new Mock<IModelProvider>();
            _mockLogger = new Mock<ILogger<RoslynCompilationService>>();
            _compilationService = new RoslynCompilationService(_mockAiProvider.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CompileToAssemblyAsync_ValidCode_ReturnsSuccess()
        {
            // Arrange
            var validCode = @"
using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;

public partial class TestPlugin : IPlugin
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

            // Act
            var result = await _compilationService.CompileToAssemblyAsync(validCode);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.True(result.Assembly.Length > 0);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task CompileToAssemblyAsync_InvalidCode_ReturnsFailure()
        {
            // Arrange
            var invalidCode = @"
public partial class TestPlugin : IPlugin
{
    // Missing using statements and incomplete implementation
    public string Name => ""TestPlugin"";
    // Missing other required properties and methods
}";

            // Act
            var result = await _compilationService.CompileToAssemblyAsync(invalidCode);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Null(result.Assembly);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task FixAndCompileAsync_ValidFix_ReturnsSuccess()
        {
            // Arrange
            var brokenCode = @"
public partial class TestPlugin : IPlugin
{
    public string Name => ""TestPlugin"";
    // Missing other required properties
}";

            var errors = new[] { "Missing required properties" };

            var fixedCode = @"
using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;

public partial class TestPlugin : IPlugin
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

            var aiResponse = new ModelResponse
            {
                Response = $@"```csharp
{fixedCode}
```",
                Success = true
            };

            _mockAiProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            // Act
            var result = await _compilationService.FixAndCompileAsync(brokenCode, errors);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.True(result.Assembly.Length > 0);
            Assert.NotNull(result.FixedSourceCode);
            Assert.Contains("using System", result.FixedSourceCode);

            _mockAiProvider.Verify(x => x.ExecuteAsync(
                It.Is<ModelRequest>(r => r.Input.Contains("Fix the following C# compilation errors")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task FixAndCompileAsync_EmptyFixResponse_ReturnsFailure()
        {
            // Arrange
            var brokenCode = "invalid code";
            var errors = new[] { "Syntax error" };

            var aiResponse = new ModelResponse
            {
                Response = "I can't fix this code.",
                Success = true
            };

            _mockAiProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            // Act
            var result = await _compilationService.FixAndCompileAsync(brokenCode, errors);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Null(result.Assembly);
            Assert.Contains("Syntax error", result.Errors);
        }

        [Fact]
        public async Task CompileToAssemblyAsync_EmptyCode_ReturnsFailure()
        {
            // Arrange
            var emptyCode = "";

            // Act
            var result = await _compilationService.CompileToAssemblyAsync(emptyCode);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Null(result.Assembly);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task CompileToAssemblyAsync_NullCode_ReturnsFailure()
        {
            // Arrange
            string? nullCode = null;

            // Act
            var result = await _compilationService.CompileToAssemblyAsync(nullCode!);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Null(result.Assembly);
            Assert.NotEmpty(result.Errors);
        }
    }
}
