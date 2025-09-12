using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Services;
using Nexo.Core.Application.Services.Extensions;
using Nexo.Core.Domain.Models.Extensions;
using Nexo.Core.Domain.Enums.Extensions;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Results;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Core.Application.Tests.Services.Extensions
{
    public class BasicExtensionGeneratorTests
    {
        private readonly Mock<ILogger<BasicExtensionGenerator>> _mockLogger;
        private readonly Mock<IAIRuntimeSelector> _mockAIRuntimeSelector;
        private readonly Mock<IAIEngine> _mockAIEngine;
        private readonly BasicExtensionGenerator _generator;

        public BasicExtensionGeneratorTests()
        {
            _mockLogger = new Mock<ILogger<BasicExtensionGenerator>>();
            _mockAIRuntimeSelector = new Mock<IAIRuntimeSelector>();
            _mockAIEngine = new Mock<IAIEngine>();
            _generator = new BasicExtensionGenerator(_mockLogger.Object, _mockAIRuntimeSelector.Object);
        }

        [Fact]
        public async Task GenerateAsync_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new ExtensionRequest
            {
                Name = "TestExtension",
                Description = "Test extension description",
                Type = ExtensionType.Analyzer
            };

            var mockResponse = @"```csharp
using Nexo.Feature.Plugin.Interfaces;

public class TestExtension : IPlugin
{
    public string Name => ""TestExtension"";
    public string Version => ""1.0.0"";
    public string Description => ""Test extension description"";
    public string Author => ""System"";
    public bool IsEnabled => true;

    public Task InitializeAsync() => Task.CompletedTask;
    public Task ShutdownAsync() => Task.CompletedTask;
}
```";

            var mockCodeResult = new CodeGenerationResult
            {
                GeneratedCode = mockResponse
            };

            _mockAIEngine.Setup(x => x.EngineInfo).Returns(new AIEngineInfo
            {
                EngineType = AIEngineType.CodeLlama
            });

            _mockAIRuntimeSelector.Setup(x => x.SelectBestEngineAsync(It.IsAny<AIOperationContext>()))
                .ReturnsAsync(_mockAIEngine.Object);

            _mockAIEngine.Setup(x => x.GenerateCodeAsync(It.IsAny<CodeGenerationRequest>()))
                .ReturnsAsync(mockCodeResult);

            // Act
            var result = await _generator.GenerateAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Code);
            Assert.Contains("TestExtension", result.Code);
            Assert.Contains("IPlugin", result.Code);
            Assert.Equal("TestExtension.cs", result.FileName);
            Assert.Equal(".cs", result.FileExtension);
        }

        [Fact]
        public async Task GenerateAsync_WithInvalidRequest_ShouldReturnFailure()
        {
            // Arrange
            var request = new ExtensionRequest
            {
                Name = "", // Invalid - empty name
                Description = "Test description",
                Type = ExtensionType.Analyzer
            };

            // Act
            var result = await _generator.GenerateAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Empty(result.Code);
            Assert.Equal("Extension.cs", result.FileName);
        }

        [Fact]
        public async Task GenerateAsync_WithEmptyResponse_ShouldReturnFailure()
        {
            // Arrange
            var request = new ExtensionRequest
            {
                Name = "TestExtension",
                Description = "Test description",
                Type = ExtensionType.Analyzer
            };

            var mockCodeResult = new CodeGenerationResult
            {
                GeneratedCode = string.Empty
            };

            _mockAIEngine.Setup(x => x.EngineInfo).Returns(new AIEngineInfo
            {
                EngineType = AIEngineType.CodeLlama
            });

            _mockAIRuntimeSelector.Setup(x => x.SelectBestEngineAsync(It.IsAny<AIOperationContext>()))
                .ReturnsAsync(_mockAIEngine.Object);

            _mockAIEngine.Setup(x => x.GenerateCodeAsync(It.IsAny<CodeGenerationRequest>()))
                .ReturnsAsync(mockCodeResult);

            // Act
            var result = await _generator.GenerateAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Empty(result.Code);
        }

        [Fact]
        public async Task GenerateAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var request = new ExtensionRequest
            {
                Name = "TestExtension",
                Description = "Test description",
                Type = ExtensionType.Analyzer
            };

            _mockAIRuntimeSelector.Setup(x => x.SelectBestEngineAsync(It.IsAny<AIOperationContext>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _generator.GenerateAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Empty(result.Code);
        }

        [Fact]
        public async Task GenerateAsync_ShouldExtractCodeFromResponse()
        {
            // Arrange
            var request = new ExtensionRequest
            {
                Name = "TestExtension",
                Description = "Test description",
                Type = ExtensionType.Analyzer
            };

            var mockResponse = @"Here's your code:

```csharp
public class TestExtension : IPlugin
{
    public string Name => ""TestExtension"";
}
```

This is a complete implementation.";

            var mockCodeResult = new CodeGenerationResult
            {
                GeneratedCode = mockResponse
            };

            _mockAIEngine.Setup(x => x.EngineInfo).Returns(new AIEngineInfo
            {
                EngineType = AIEngineType.CodeLlama
            });

            _mockAIRuntimeSelector.Setup(x => x.SelectBestEngineAsync(It.IsAny<AIOperationContext>()))
                .ReturnsAsync(_mockAIEngine.Object);

            _mockAIEngine.Setup(x => x.GenerateCodeAsync(It.IsAny<CodeGenerationRequest>()))
                .ReturnsAsync(mockCodeResult);

            // Act
            var result = await _generator.GenerateAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("public class TestExtension", result.Code);
            Assert.DoesNotContain("Here's your code:", result.Code);
            Assert.DoesNotContain("This is a complete implementation.", result.Code);
        }

        [Fact]
        public async Task GenerateAsync_WithNoCodeBlocks_ShouldReturnFullResponse()
        {
            // Arrange
            var request = new ExtensionRequest
            {
                Name = "TestExtension",
                Description = "Test description",
                Type = ExtensionType.Analyzer
            };

            var mockResponse = "public class TestExtension : IPlugin { }";

            var mockCodeResult = new CodeGenerationResult
            {
                GeneratedCode = mockResponse
            };

            _mockAIEngine.Setup(x => x.EngineInfo).Returns(new AIEngineInfo
            {
                EngineType = AIEngineType.CodeLlama
            });

            _mockAIRuntimeSelector.Setup(x => x.SelectBestEngineAsync(It.IsAny<AIOperationContext>()))
                .ReturnsAsync(_mockAIEngine.Object);

            _mockAIEngine.Setup(x => x.GenerateCodeAsync(It.IsAny<CodeGenerationRequest>()))
                .ReturnsAsync(mockCodeResult);

            // Act
            var result = await _generator.GenerateAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(mockResponse, result.Code);
        }
    }
}
