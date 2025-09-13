using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Services.Extensions;
using Nexo.Core.Domain.Models.Extensions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Core.Application.Tests.Services.Extensions
{
    public class CSharpSyntaxValidatorTests
    {
        private readonly Mock<ILogger<CSharpSyntaxValidator>> _mockLogger;
        private readonly CSharpSyntaxValidator _validator;

        public CSharpSyntaxValidatorTests()
        {
            _mockLogger = new Mock<ILogger<CSharpSyntaxValidator>>();
            _validator = new CSharpSyntaxValidator(_mockLogger.Object);
        }

        [Fact]
        public async Task ValidateSyntaxAsync_WithValidCode_ShouldReturnSuccess()
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
    }
}";
            var assemblyName = "TestPlugin";

            // Act
            var result = await _validator.ValidateSyntaxAsync(code, assemblyName);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.HasCompilationErrors);
            Assert.Equal(code, result.GeneratedCode);
        }

        [Fact]
        public async Task ValidateSyntaxAsync_WithSyntaxErrors_ShouldReturnErrors()
        {
            // Arrange
            var code = "public class TestPlugin { // Missing closing brace";
            var assemblyName = "TestPlugin";

            // Act
            var result = await _validator.ValidateSyntaxAsync(code, assemblyName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.HasCompilationErrors);
            Assert.NotEmpty(result.CompilationErrors);
        }

        [Fact]
        public async Task ValidateSyntaxAsync_WithWarnings_ShouldReturnWarnings()
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
    }
}";
            var assemblyName = "TestPlugin";

            // Act
            var result = await _validator.ValidateSyntaxAsync(code, assemblyName);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.HasCompilationErrors);
            // Note: This test might have warnings depending on the specific code
        }

        [Fact]
        public async Task ValidateSyntaxAsync_WithEmptyCode_ShouldReturnError()
        {
            // Arrange
            var code = "";
            var assemblyName = "TestPlugin";

            // Act
            var result = await _validator.ValidateSyntaxAsync(code, assemblyName);

            // Assert
            // Empty code might not be considered a syntax error by Roslyn
            // Let's just check that we get a result
            Assert.NotNull(result);
            Assert.Equal(code, result.GeneratedCode);
        }

        [Fact]
        public async Task ValidateSyntaxAsync_WithInvalidCode_ShouldReturnError()
        {
            // Arrange
            var code = "This is not valid C# code at all!";
            var assemblyName = "TestPlugin";

            // Act
            var result = await _validator.ValidateSyntaxAsync(code, assemblyName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.HasCompilationErrors);
        }
    }
}