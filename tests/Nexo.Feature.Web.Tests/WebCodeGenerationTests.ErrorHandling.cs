using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Web.Models;
using Nexo.Feature.Web.Enums;
using Nexo.Feature.Web.Services;
using Nexo.Feature.Web.UseCases;
using Moq;
using Nexo.Feature.Web.Interfaces;

namespace Nexo.Feature.Web.Tests
{
    /// <summary>
    /// Error handling tests for web code generation functionality
    /// </summary>
    public partial class WebCodeGenerationTests
    {
        [Fact]
        public async Task WebCodeGenerator_WithInvalidRequest_ReturnsFailure()
        {
            // Arrange
            var mockTemplateProvider = new Mock<IFrameworkTemplateProvider>();
            var mockWasmOptimizer = new Mock<IWebAssemblyOptimizer>();
            var logger = NullLogger<WebCodeGenerator>.Instance;

            var generator = new WebCodeGenerator(logger, mockTemplateProvider.Object, mockWasmOptimizer.Object);

            var request = new WebCodeGenerationRequest
            {
                ComponentName = "", // Invalid - empty name
                TargetPath = "" // Invalid - empty path
            };

            // Act
            var result = await generator.GenerateCodeAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid request parameters", result.Message);
        }

        [Fact]
        public void FrameworkTemplateProvider_WithInvalidFramework_ReturnsDefaultTemplate()
        {
            // Arrange
            var logger = NullLogger<FrameworkTemplateProvider>.Instance;
            var provider = new FrameworkTemplateProvider(logger);

            // Act
            var template = provider.GetTemplate(WebFrameworkType.React, WebComponentType.HigherOrder);

            // Assert
            Assert.NotNull(template);
            Assert.Contains("Default template", template);
        }

        [Fact]
        public async Task GenerateWebCodeUseCase_WithInvalidRequest_ReturnsFailure()
        {
            // Arrange
            var mockCodeGenerator = new Mock<IWebCodeGenerator>();
            var mockWasmOptimizer = new Mock<IWebAssemblyOptimizer>();
            var logger = NullLogger<GenerateWebCodeUseCase>.Instance;

            mockCodeGenerator.Setup(x => x.ValidateRequest(It.IsAny<WebCodeGenerationRequest>()))
                .Returns(false);

            var useCase = new GenerateWebCodeUseCase(logger, mockCodeGenerator.Object, mockWasmOptimizer.Object);

            var request = new WebCodeGenerationRequest
            {
                ComponentName = "", // Invalid
                TargetPath = "" // Invalid
            };

            // Act
            var result = await useCase.ExecuteAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid request parameters", result.Message);
            mockCodeGenerator.Verify(x => x.GenerateCodeAsync(It.IsAny<WebCodeGenerationRequest>()), Times.Never);
        }
    }
}
