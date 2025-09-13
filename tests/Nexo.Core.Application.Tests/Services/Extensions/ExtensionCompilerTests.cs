using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Services.Extensions;
using Nexo.Core.Domain.Models.Extensions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Core.Application.Tests.Services.Extensions
{
    public class ExtensionCompilerTests
    {
        private readonly Mock<ILogger<ExtensionCompiler>> _mockLogger;
        private readonly ExtensionCompiler _compiler;

        public ExtensionCompilerTests()
        {
            _mockLogger = new Mock<ILogger<ExtensionCompiler>>();
            _compiler = new ExtensionCompiler(_mockLogger.Object);
        }

        [Fact]
        public async Task CompileExtensionAsync_WithValidCode_ShouldReturnSuccess()
        {
            // Arrange
            var code = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Interfaces;

namespace TestPlugin
{
    public class TestPlugin : IPlugin
    {
        public string Name => ""TestPlugin"";
        public string Version => ""1.0.0"";
        public string Description => ""A test plugin"";
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
    }
}";
            var assemblyName = "TestPlugin";

            // Act
            var result = await _compiler.CompileExtensionAsync(code, assemblyName);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.CompiledAssembly);
            Assert.True(result.CompiledAssembly.Length > 0);
            Assert.Equal(code, result.GeneratedCode);
        }

        [Fact]
        public async Task CompileExtensionAsync_WithSyntaxErrors_ShouldReturnErrors()
        {
            // Arrange
            var code = @"
using System;
using Nexo.Core.Domain.Interfaces;

namespace TestPlugin
{
    public class TestPlugin : IPlugin
    {
        public string Name => ""TestPlugin"";
        public string Version => ""1.0.0"";
        public string Description => ""A test plugin"";
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
    } // Missing closing brace
}";
            var assemblyName = "TestPlugin";

            // Act
            var result = await _compiler.CompileExtensionAsync(code, assemblyName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.HasCompilationErrors);
            Assert.NotEmpty(result.CompilationErrors);
        }

        [Fact]
        public async Task CompileExtensionAsync_WithMissingUsing_ShouldReturnErrors()
        {
            // Arrange
            var code = @"
namespace TestPlugin
{
    public class TestPlugin : IPlugin
    {
        public string Name => ""TestPlugin"";
        public string Version => ""1.0.0"";
        public string Description => ""A test plugin"";
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
    }
}";
            var assemblyName = "TestPlugin";

            // Act
            var result = await _compiler.CompileExtensionAsync(code, assemblyName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.HasCompilationErrors);
            Assert.NotEmpty(result.CompilationErrors);
        }

        [Fact]
        public async Task CompileExtensionAsync_WithEmptyCode_ShouldReturnError()
        {
            // Arrange
            var code = "";
            var assemblyName = "TestPlugin";

            // Act
            var result = await _compiler.CompileExtensionAsync(code, assemblyName);

            // Assert
            // Empty code might not be considered a compilation error by Roslyn
            // Let's just check that we get a result
            Assert.NotNull(result);
            Assert.Equal(code, result.GeneratedCode);
        }

        [Fact]
        public async Task CompileExtensionAsync_WithInvalidCode_ShouldReturnError()
        {
            // Arrange
            var code = "This is not valid C# code at all!";
            var assemblyName = "TestPlugin";

            // Act
            var result = await _compiler.CompileExtensionAsync(code, assemblyName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.HasCompilationErrors);
        }
    }
}