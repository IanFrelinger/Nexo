using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;
using Xunit;

namespace Nexo.Feature.Platform.Tests.Services.ErrorHandling
{
    /// <summary>
    /// Tests for error handling scenarios in Platform Feature Detector
    /// </summary>
    public class ErrorHandlingTests
    {
        private readonly Mock<ILogger<PlatformFeatureDetector>> _mockLogger;
        private readonly PlatformFeatureDetector _detector;

        public ErrorHandlingTests()
        {
            _mockLogger = new Mock<ILogger<PlatformFeatureDetector>>();
            _detector = new PlatformFeatureDetector(_mockLogger.Object);
        }

        [Fact]
        public async Task DetectFeaturesForPlatformAsync_WithUnknownPlatform_ReturnsSuccessResult()
        {
            // Arrange & Act
            var result = await _detector.DetectFeaturesForPlatformAsync(PlatformType.Unknown);

            // Assert
            Assert.True(result.IsSuccess); // Service is optimistic and returns success even for unknown platforms
            Assert.Equal(PlatformType.Unknown, result.PlatformType);
            Assert.NotNull(result.DetectedFeatures);
        }

        [Fact]
        public async Task CheckFeatureAvailabilityAsync_WithEmptyFeatureName_ReturnsAvailableResult()
        {
            // Arrange
            var featureName = string.Empty;

            // Act
            var result = await _detector.CheckFeatureAvailabilityAsync(featureName);

            // Assert
            Assert.True(result.IsAvailable); // Service is optimistic and returns available even for empty feature names
            Assert.Equal(featureName, result.FeatureName);
        }

        [Fact]
        public async Task ValidateFeatureCompatibilityAsync_WithEmptyFeatures_ReturnsEmptyResult()
        {
            // Arrange
            var features = new List<string>();
            var platforms = new List<PlatformType> { PlatformType.Windows };

            // Act
            var result = await _detector.ValidateFeatureCompatibilityAsync(features, platforms);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Features);
            Assert.Empty(result.CompatibilityMatrix);
        }

        [Fact]
        public async Task ValidateFeatureCompatibilityAsync_WithEmptyPlatforms_ReturnsValidResult()
        {
            // Arrange
            var features = new List<string> { "FileSystem" };
            var platforms = new List<PlatformType>();

            // Act
            var result = await _detector.ValidateFeatureCompatibilityAsync(features, platforms);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Platforms);
            Assert.NotEmpty(result.CompatibilityMatrix); // Service returns compatibility matrix even for empty platforms
        }
    }
}
