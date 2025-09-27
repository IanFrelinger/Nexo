using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nexo.Feature.Platform.Tests.Services
{
    /// <summary>
    /// Error handling tests for Android code generator.
    /// </summary>
    public partial class AndroidCodeGeneratorTests
    {
        [Fact]
        public async Task GenerateJetpackComposeCodeAsync_WithNullApplicationLogic_ThrowsArgumentNullException()
        {
            // Arrange
            StandardizedApplicationLogic applicationLogic = null;
            var androidOptions = new AndroidGenerationOptions();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _androidCodeGenerator.GenerateJetpackComposeCodeAsync(applicationLogic, androidOptions));
        }

        [Fact]
        public async Task GenerateJetpackComposeCodeAsync_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            var applicationLogic = CreateValidApplicationLogic();
            AndroidGenerationOptions androidOptions = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _androidCodeGenerator.GenerateJetpackComposeCodeAsync(applicationLogic, androidOptions));
        }

        [Fact]
        public async Task ValidateAndroidCodeAsync_WithInvalidCode_ReturnsInvalidResult()
        {
            // Arrange
            var androidCode = CreateInvalidAndroidGeneratedCode();
            var validationOptions = new AndroidValidationOptions();

            // Act
            var result = await _androidCodeGenerator.ValidateAndroidCodeAsync(androidCode, validationOptions);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(0.0, result.ValidationScore);
            Assert.Equal("Android code validation failed", result.Message);
            Assert.NotEmpty(result.ValidationErrors);
        }
    }
}
