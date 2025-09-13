using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Application.Services.Extensions;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Models.Extensions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Core.Application.Tests.Services.Extensions
{
    public class AIExtensionGeneratorTests
    {
        private readonly Mock<ILogger<AIExtensionGenerator>> _mockLogger;
        private readonly Mock<IAIEngine> _mockAIEngine;
        private readonly Mock<ICSharpSyntaxValidator> _mockSyntaxValidator;
        private readonly Mock<IExtensionCompiler> _mockCompiler;
        private readonly AIExtensionGenerator _generator;

        public AIExtensionGeneratorTests()
        {
            _mockLogger = new Mock<ILogger<AIExtensionGenerator>>();
            _mockAIEngine = new Mock<IAIEngine>();
            _mockSyntaxValidator = new Mock<ICSharpSyntaxValidator>();
            _mockCompiler = new Mock<IExtensionCompiler>();
            
            _generator = new AIExtensionGenerator(
                _mockLogger.Object,
                _mockAIEngine.Object,
                _mockSyntaxValidator.Object,
                _mockCompiler.Object);
        }

        [Fact]
        public async Task GenerateExtensionAsync_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new ExtensionRequest
            {
                Name = "TestPlugin",
                Description = "A test plugin",
                Type = Nexo.Core.Domain.Enums.Extensions.ExtensionType.Custom
            };

            var mockCodeResult = new CodeGenerationResult
            {
                GeneratedCode = "public class TestPlugin : IPlugin { }"
            };

            var mockValidationResult = new ExtensionGenerationResult
            {
                IsSuccess = true,
                GeneratedCode = "public class TestPlugin : IPlugin { }"
            };

            var mockCompilationResult = new ExtensionGenerationResult
            {
                IsSuccess = true,
                GeneratedCode = "public class TestPlugin : IPlugin { }",
                CompiledAssembly = new byte[] { 1, 2, 3, 4 }
            };

            _mockAIEngine.Setup(x => x.GenerateCodeAsync(It.IsAny<CodeGenerationRequest>()))
                .ReturnsAsync(mockCodeResult);
            
            _mockSyntaxValidator.Setup(x => x.ValidateSyntaxAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockValidationResult);
            
            _mockCompiler.Setup(x => x.CompileExtensionAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockCompilationResult);

            // Act
            var result = await _generator.GenerateExtensionAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("TestPlugin", result.OriginalRequest.Name);
            Assert.NotNull(result.GeneratedCode);
            Assert.NotNull(result.CompiledAssembly);
        }

        [Fact]
        public async Task GenerateExtensionAsync_WithSyntaxErrors_ShouldReturnFailure()
        {
            // Arrange
            var request = new ExtensionRequest
            {
                Name = "TestPlugin",
                Description = "A test plugin",
                Type = Nexo.Core.Domain.Enums.Extensions.ExtensionType.Custom
            };

            var mockCodeResult = new CodeGenerationResult
            {
                GeneratedCode = "public class TestPlugin : IPlugin { }"
            };

            var mockValidationResult = new ExtensionGenerationResult
            {
                IsSuccess = false,
                GeneratedCode = "public class TestPlugin : IPlugin { }"
            };
            mockValidationResult.AddCompilationError("Syntax error", 1, 1, "CS1001");

            _mockAIEngine.Setup(x => x.GenerateCodeAsync(It.IsAny<CodeGenerationRequest>()))
                .ReturnsAsync(mockCodeResult);
            
            _mockSyntaxValidator.Setup(x => x.ValidateSyntaxAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(mockValidationResult);

            // Act
            var result = await _generator.GenerateExtensionAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.True(result.HasCompilationErrors);
            Assert.Single(result.CompilationErrors);
        }

        [Fact]
        public async Task GenerateCodeAsync_WithValidRequest_ShouldReturnCode()
        {
            // Arrange
            var request = new ExtensionRequest
            {
                Name = "TestPlugin",
                Description = "A test plugin",
                Type = Nexo.Core.Domain.Enums.Extensions.ExtensionType.Custom
            };

            var mockCodeResult = new CodeGenerationResult
            {
                GeneratedCode = "public class TestPlugin : IPlugin { }"
            };

            _mockAIEngine.Setup(x => x.GenerateCodeAsync(It.IsAny<CodeGenerationRequest>()))
                .ReturnsAsync(mockCodeResult);

            // Act
            var result = await _generator.GenerateCodeAsync(request);

            // Assert
            Assert.Equal("public class TestPlugin : IPlugin { }", result);
        }

        [Fact]
        public async Task ValidateCodeAsync_WithValidCode_ShouldReturnSuccess()
        {
            // Arrange
            var code = "public class TestPlugin : IPlugin { }";
            var mockValidationResult = new ExtensionGenerationResult
            {
                IsSuccess = true,
                GeneratedCode = code
            };

            _mockSyntaxValidator.Setup(x => x.ValidateSyntaxAsync(code, "temp"))
                .ReturnsAsync(mockValidationResult);

            // Act
            var result = await _generator.ValidateCodeAsync(code);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task CompileCodeAsync_WithValidCode_ShouldReturnCompilationResult()
        {
            // Arrange
            var code = "public class TestPlugin : IPlugin { }";
            var request = new ExtensionRequest
            {
                Name = "TestPlugin",
                Description = "A test plugin"
            };

            var mockCompilationResult = new ExtensionGenerationResult
            {
                IsSuccess = true,
                GeneratedCode = code,
                CompiledAssembly = new byte[] { 1, 2, 3, 4 }
            };

            _mockCompiler.Setup(x => x.CompileExtensionAsync(code, request.Name))
                .ReturnsAsync(mockCompilationResult);

            // Act
            var result = await _generator.CompileCodeAsync(code, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.CompiledAssembly);
        }
    }
}