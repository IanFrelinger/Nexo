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

namespace Nexo.Feature.Platform.Tests.Services.CrossPlatform
{
    /// <summary>
    /// Tests for cross-platform scenarios in Platform Feature Detector
    /// </summary>
    public class CrossPlatformTests
    {
        private readonly Mock<ILogger<PlatformFeatureDetector>> _mockLogger;
        private readonly PlatformFeatureDetector _detector;

        public CrossPlatformTests()
        {
            _mockLogger = new Mock<ILogger<PlatformFeatureDetector>>();
            _detector = new PlatformFeatureDetector(_mockLogger.Object);
        }

        [Theory]
        [InlineData(PlatformType.Windows)]
        [InlineData(PlatformType.MacOS)]
        [InlineData(PlatformType.Linux)]
        public async Task PlatformFeatureDetection_CrossPlatformScenarios_WorkCorrectly(PlatformType platformType)
        {
            // Arrange & Act
            var features = await _detector.DetectFeaturesForPlatformAsync(platformType);
            var capabilities = await _detector.DetectPlatformCapabilitiesAsync(platformType);
            var recommendations = await _detector.GetRecommendedFeaturesAsync(platformType);

            // Assert
            Assert.True(features.IsSuccess);
            Assert.Equal(platformType, features.PlatformType);
            Assert.True(features.DetectedFeatures.Count > 0);

            Assert.True(capabilities.IsSuccess);
            Assert.Equal(platformType, capabilities.PlatformType);
            Assert.True(capabilities.Capabilities.Count > 0);

            Assert.True(recommendations.IsSuccess);
            Assert.Equal(platformType, recommendations.PlatformType);
        }

        [Fact]
        public async Task PlatformFeatureDetection_CompleteWorkflow_WorksCorrectly()
        {
            // Arrange
            var platform = PlatformType.Windows;
            var features = new List<string> { "FileSystem", "NetworkAccess" };

            // Act
            var detectionResult = await _detector.DetectFeaturesForPlatformAsync(platform);
            var capabilitiesResult = await _detector.DetectPlatformCapabilitiesAsync(platform);
            var availabilityResult = await _detector.CheckFeatureAvailabilityAsync("FileSystem");
            var compatibilityResult = await _detector.ValidateFeatureCompatibilityAsync(features, new List<PlatformType> { platform });
            var recommendationsResult = await _detector.GetRecommendedFeaturesAsync(platform);

            // Assert
            Assert.True(detectionResult.IsSuccess);
            Assert.True(capabilitiesResult.IsSuccess);
            Assert.NotNull(availabilityResult);
            Assert.NotNull(compatibilityResult);
            Assert.True(recommendationsResult.IsSuccess);
        }
    }
}
