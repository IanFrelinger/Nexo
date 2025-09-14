using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Generation;
using Xunit;

namespace Nexo.Infrastructure.Tests.ToolGeneration
{
    /// <summary>
    /// Tests for CodeGenerator
    /// </summary>
    public class CodeGeneratorTests
    {
        private readonly Mock<IModelProvider> _mockAiProvider;
        private readonly Mock<ILogger<CodeGenerator>> _mockLogger;
        private readonly CodeGenerator _codeGenerator;

        public CodeGeneratorTests()
        {
            _mockAiProvider = new Mock<IModelProvider>();
            _mockLogger = new Mock<ILogger<CodeGenerator>>();
            _codeGenerator = new CodeGenerator(_mockAiProvider.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GenerateFromDescriptionAsync_ValidDescription_ReturnsGeneratedCode()
        {
            // Arrange
            var description = "Create a JSON formatter";
            var aiResponse = new ModelResponse
            {
                Response = @"```csharp
public class JsonFormatter : IPlugin
{
    public string Name => ""JsonFormatter"";
    public string Version => ""1.0"";
    public string Description => ""Formats JSON data"";
    public string Author => ""AI Generated"";
    public bool IsEnabled => true;

    public async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // Initialization logic
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        // Cleanup logic
    }

    public async Task<PluginResult> ExecuteAsync(string[] args)
    {
        // Implementation logic
        return new PluginResult { Success = true, Message = ""JSON formatted"" };
    }
}
```",
                Success = true
            };

            _mockAiProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            // Act
            var result = await _codeGenerator.GenerateFromDescriptionAsync(description);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("public class JsonFormatter", result.SourceCode);
            Assert.Contains("IPlugin", result.SourceCode);
            Assert.Contains("ExecuteAsync", result.SourceCode);
            Assert.True(result.IsWrappedInPlugin);
            Assert.Equal("JsonFormatter", result.ToolName);
            Assert.Contains("JSON", result.Description);

            _mockAiProvider.Verify(x => x.ExecuteAsync(
                It.Is<ModelRequest>(r => r.Input.Contains(description) && r.Temperature == 0.2),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GenerateFromDescriptionAsync_EmptyResponse_ThrowsException()
        {
            // Arrange
            var description = "Create a JSON formatter";
            var aiResponse = new ModelResponse
            {
                Response = "",
                Success = true
            };

            _mockAiProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _codeGenerator.GenerateFromDescriptionAsync(description));
        }

        [Fact]
        public async Task GenerateFromDescriptionAsync_NoCodeInResponse_ThrowsException()
        {
            // Arrange
            var description = "Create a JSON formatter";
            var aiResponse = new ModelResponse
            {
                Response = "I can't generate code for that request.",
                Success = true
            };

            _mockAiProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _codeGenerator.GenerateFromDescriptionAsync(description));
        }

        [Fact]
        public async Task GenerateFromPromptAsync_ValidPrompt_ReturnsGeneratedCode()
        {
            // Arrange
            var prompt = "Generate a calculator plugin";
            var aiResponse = new ModelResponse
            {
                Response = @"```csharp
public class Calculator : IPlugin
{
    public string Name => ""Calculator"";
    public string Version => ""1.0"";
    public string Description => ""Basic calculator operations"";
    public string Author => ""AI Generated"";
    public bool IsEnabled => true;

    public async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
    }

    public async Task<PluginResult> ExecuteAsync(string[] args)
    {
        return new PluginResult { Success = true, Message = ""Calculated"" };
    }
}
```",
                Success = true
            };

            _mockAiProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            // Act
            var result = await _codeGenerator.GenerateFromPromptAsync(prompt);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("public class Calculator", result.SourceCode);
            Assert.True(result.IsWrappedInPlugin);
            Assert.Equal("Calculator", result.ToolName);
        }

        [Theory]
        [InlineData("Create a JSON formatter", "JsonFormatter")]
        [InlineData("Build an API tester", "ApiTester")]
        [InlineData("Make a CSV converter", "CsvConverter")]
        public async Task GenerateFromDescriptionAsync_ExtractsCorrectToolName(string description, string expectedName)
        {
            // Arrange
            var aiResponse = new ModelResponse
            {
                Response = $@"```csharp
public class {expectedName} : IPlugin
{{
    public string Name => ""{expectedName}"";
    // ... rest of implementation
}}
```",
                Success = true
            };

            _mockAiProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<ModelRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(aiResponse);

            // Act
            var result = await _codeGenerator.GenerateFromDescriptionAsync(description);

            // Assert
            Assert.Equal(expectedName, result.ToolName);
        }
    }
}
