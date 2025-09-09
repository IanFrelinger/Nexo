using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Models.Platform;
using Nexo.Infrastructure.Services.Platform;
using Xunit;

namespace Nexo.Infrastructure.Tests.Services.Platform
{
    /// <summary>
    /// Tests for platform feature detection service.
    /// </summary>
    public class PlatformFeatureDetectionServiceTests
    {
        private readonly Mock<ILogger<PlatformFeatureDetectionService>> _mockLogger;
        private readonly Mock<IModelOrchestrator> _mockModelOrchestrator;

        public PlatformFeatureDetectionServiceTests()
        {
            _mockLogger = new Mock<ILogger<PlatformFeatureDetectionService>>();
            _mockModelOrchestrator = new Mock<IModelOrchestrator>();
        }

        [Fact]
        public async Task DetectCapabilitiesAsync_ShouldReturnCapabilities()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var platform = "iOS";

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Platform capabilities detected" });

            // Act
            var result = await service.DetectCapabilitiesAsync(platform);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(platform, result.Platform);
            Assert.True(result.Success);
            Assert.NotNull(result.UICapabilities);
            Assert.NotNull(result.DataCapabilities);
            Assert.NotNull(result.NetworkCapabilities);
            Assert.NotNull(result.HardwareCapabilities);
            Assert.NotNull(result.SecurityCapabilities);
            Assert.NotNull(result.PerformanceCapabilities);
        }

        [Fact]
        public async Task DetectCapabilitiesAsync_ShouldHandleErrors()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var platform = "iOS";

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("AI service error"));

            // Act
            var result = await service.DetectCapabilitiesAsync(platform);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(platform, result.Platform);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("AI service error", result.ErrorMessage);
        }

        [Fact]
        public async Task MapFeatureAvailabilityAsync_ShouldReturnFeatureMap()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var features = new[] { "Camera", "GPS", "Bluetooth" };
            var platforms = new[] { "iOS", "Android", "Web" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Feature availability mapped" });

            // Act
            var result = await service.MapFeatureAvailabilityAsync(features, platforms);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.FeatureMappings);
            Assert.Equal(features.Length, result.FeatureMappings.Count());
        }

        [Fact]
        public async Task MapFeatureAvailabilityAsync_ShouldHandleErrors()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var features = new[] { "Camera", "GPS" };
            var platforms = new[] { "iOS", "Android" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("AI service error"));

            // Act
            var result = await service.MapFeatureAvailabilityAsync(features, platforms);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("AI service error", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateFeatureCompatibilityAsync_ShouldReturnCompatibilityReport()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var features = new[] { "Camera", "GPS" };
            var platforms = new[] { "iOS", "Android" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Feature compatibility validated" });

            // Act
            var result = await service.ValidateFeatureCompatibilityAsync(features, platforms);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.CompatibilityIssues);
        }

        [Fact]
        public async Task ValidateFeatureCompatibilityAsync_ShouldHandleErrors()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var features = new[] { "Camera" };
            var platforms = new[] { "iOS" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("AI service error"));

            // Act
            var result = await service.ValidateFeatureCompatibilityAsync(features, platforms);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("AI service error", result.ErrorMessage);
        }

        [Fact]
        public async Task GetPlatformRecommendationsAsync_ShouldReturnRecommendations()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var features = new[] { "Camera", "GPS" };
            var targetPlatform = "iOS";

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Platform recommendations generated" });

            // Act
            var result = await service.GetPlatformRecommendationsAsync(features, targetPlatform);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetPlatform, result.TargetPlatform);
            Assert.True(result.Success);
            Assert.NotNull(result.FeatureRecommendations);
        }

        [Fact]
        public async Task GetPlatformRecommendationsAsync_ShouldHandleErrors()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var features = new[] { "Camera" };
            var targetPlatform = "iOS";

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("AI service error"));

            // Act
            var result = await service.GetPlatformRecommendationsAsync(features, targetPlatform);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetPlatform, result.TargetPlatform);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("AI service error", result.ErrorMessage);
        }

        [Theory]
        [InlineData("iOS")]
        [InlineData("Android")]
        [InlineData("Web")]
        [InlineData("Desktop")]
        public async Task DetectCapabilitiesAsync_ShouldWorkForAllPlatforms(string platform)
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = $"Capabilities for {platform}" });

            // Act
            var result = await service.DetectCapabilitiesAsync(platform);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(platform, result.Platform);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task DetectCapabilitiesAsync_ShouldCallModelOrchestratorMultipleTimes()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var platform = "iOS";

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Capabilities detected" });

            // Act
            var result = await service.DetectCapabilitiesAsync(platform);

            // Assert
            _mockModelOrchestrator.Verify(
                x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.AtLeast(6)); // UI, Data, Network, Hardware, Security, Performance
        }

        [Fact]
        public async Task MapFeatureAvailabilityAsync_ShouldCallModelOrchestratorForEachFeature()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var features = new[] { "Camera", "GPS", "Bluetooth" };
            var platforms = new[] { "iOS", "Android" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Feature availability determined" });

            // Act
            var result = await service.MapFeatureAvailabilityAsync(features, platforms);

            // Assert
            _mockModelOrchestrator.Verify(
                x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.AtLeast(features.Length * platforms.Length));
        }

        [Fact]
        public async Task ValidateFeatureCompatibilityAsync_ShouldCallModelOrchestratorForEachFeature()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var features = new[] { "Camera", "GPS" };
            var platforms = new[] { "iOS", "Android" };

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Feature compatibility validated" });

            // Act
            var result = await service.ValidateFeatureCompatibilityAsync(features, platforms);

            // Assert
            _mockModelOrchestrator.Verify(
                x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.AtLeast(features.Length));
        }

        [Fact]
        public async Task GetPlatformRecommendationsAsync_ShouldCallModelOrchestratorForEachFeature()
        {
            // Arrange
            var service = new PlatformFeatureDetectionService(_mockLogger.Object, _mockModelOrchestrator.Object);
            var features = new[] { "Camera", "GPS" };
            var targetPlatform = "iOS";

            _mockModelOrchestrator
                .Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIResponse { Content = "Platform recommendations generated" });

            // Act
            var result = await service.GetPlatformRecommendationsAsync(features, targetPlatform);

            // Assert
            _mockModelOrchestrator.Verify(
                x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.AtLeast(features.Length));
        }
    }
}
