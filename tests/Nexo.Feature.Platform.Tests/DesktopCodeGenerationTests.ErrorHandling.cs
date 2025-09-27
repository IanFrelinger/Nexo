using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Tests
{
    /// <summary>
    /// Error handling tests for desktop code generation functionality.
    /// </summary>
    public partial class DesktopCodeGenerationTests
    {
        #region Service Tests - Error Scenarios

        [Fact]
        public async Task GenerateCodeAsync_WithInvalidRequest_ReturnsFailureResult()
        {
            // Arrange
            var request = new DesktopCodeGenerationRequest
            {
                Platform = "InvalidPlatform",
                ApplicationType = "InvalidType",
                ApplicationName = "",
                TargetFramework = ""
            };

            // Act
            var result = await _desktopCodeGenerator.GenerateCodeAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid request parameters", result.Errors);
        }

        [Fact]
        public async Task GenerateCodeAsync_WithNullRequest_ReturnsFailureResult()
        {
            // Arrange
            DesktopCodeGenerationRequest request = null;

            // Act
            var result = await _desktopCodeGenerator.GenerateCodeAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid request parameters", result.Errors);
        }

        [Fact]
        public void ValidateRequest_WithInvalidRequest_ReturnsFalse()
        {
            // Arrange
            var request = new DesktopCodeGenerationRequest
            {
                Platform = "InvalidPlatform",
                ApplicationType = "InvalidType",
                ApplicationName = "",
                TargetFramework = ""
            };

            // Act
            var isValid = _desktopCodeGenerator.ValidateRequest(request);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateRequest_WithNullRequest_ReturnsFalse()
        {
            // Arrange
            DesktopCodeGenerationRequest request = null;

            // Act
            var isValid = _desktopCodeGenerator.ValidateRequest(request);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task ValidateCodeAsync_WithEmptyCode_ReturnsInvalidResult()
        {
            // Arrange
            var code = "";
            var platform = "Windows";

            // Act
            var result = await _desktopCodeGenerator.ValidateCodeAsync(code, platform);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Code is empty", result.Errors);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GenerateCodeAsync_WithException_ReturnsFailureResult()
        {
            // Arrange
            var request = new DesktopCodeGenerationRequest
            {
                Platform = "Windows",
                ApplicationType = "WPF",
                UIFramework = "WPF",
                ApplicationName = "TestApp",
                TargetFramework = "net8.0"
            };

            // Mock logger to throw exception only on LogInformation calls
            var mockLogger = new Mock<ILogger<DesktopCodeGenerator>>();
            mockLogger.Setup(l => l.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                     .Throws(new InvalidOperationException("Test exception"));

            var generator = new DesktopCodeGenerator(mockLogger.Object);

            // Act
            var result = await generator.GenerateCodeAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Test exception", result.Errors);
        }

        #endregion
    }
}
