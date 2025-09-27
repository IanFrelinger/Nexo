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

namespace Nexo.Feature.Platform.Tests.Services.Functionality
{
    /// <summary>
    /// Tests for core service functionality in Platform Feature Detector
    /// </summary>
    public partial class FunctionalityTests
    {
        private readonly Mock<ILogger<PlatformFeatureDetector>> _mockLogger;
        private readonly PlatformFeatureDetector _detector;

        public FunctionalityTests()
        {
            _mockLogger = new Mock<ILogger<PlatformFeatureDetector>>();
            _detector = new PlatformFeatureDetector(_mockLogger.Object);
        }

        [Fact]
        public async Task DetectPlatformFeaturesAsync_WithValidRequest_ReturnsSuccessResult()
        {
            // Arrange & Act
            var result = await _detector.DetectPlatformFeaturesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.DetectedFeatures);
            Assert.True(result.DetectedFeatures.Count > 0);
            Assert.NotNull(result.PlatformVersion);
            Assert.NotNull(result.Architecture);
        }

        [Fact]
        public async Task DetectFeaturesForPlatformAsync_WithWindowsPlatform_ReturnsWindowsFeatures()
        {
            // Arrange & Act
            var result = await _detector.DetectFeaturesForPlatformAsync(PlatformType.Windows);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.Windows, result.PlatformType);
            Assert.NotNull(result.DetectedFeatures);
            Assert.True(result.DetectedFeatures.Count > 0);
            
            var windowsFeatures = result.DetectedFeatures.Where(f => f.SupportedPlatforms.Contains("Windows"));
            Assert.True(windowsFeatures.Any());
        }

        [Fact]
        public async Task DetectFeaturesForPlatformAsync_WithMacOSPlatform_ReturnsMacOSFeatures()
        {
            // Arrange & Act
            var result = await _detector.DetectFeaturesForPlatformAsync(PlatformType.MacOS);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.MacOS, result.PlatformType);
            Assert.NotNull(result.DetectedFeatures);
            Assert.True(result.DetectedFeatures.Count > 0);
            
            var macosFeatures = result.DetectedFeatures.Where(f => f.SupportedPlatforms.Contains("macOS"));
            Assert.True(macosFeatures.Any());
        }

        [Fact]
        public async Task DetectFeaturesForPlatformAsync_WithLinuxPlatform_ReturnsLinuxFeatures()
        {
            // Arrange & Act
            var result = await _detector.DetectFeaturesForPlatformAsync(PlatformType.Linux);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.Linux, result.PlatformType);
            Assert.NotNull(result.DetectedFeatures);
            Assert.True(result.DetectedFeatures.Count > 0);
            
            var linuxFeatures = result.DetectedFeatures.Where(f => f.SupportedPlatforms.Contains("Linux"));
            Assert.True(linuxFeatures.Any());
        }

        [Fact]
        public async Task CheckFeatureAvailabilityAsync_WithValidFeature_ReturnsAvailabilityResult()
        {
            // Arrange
            var featureName = "FileSystem";

            // Act
            var result = await _detector.CheckFeatureAvailabilityAsync(featureName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(featureName, result.FeatureName);
            Assert.NotNull(result.Reason);
            Assert.NotNull(result.AlternativeFeatures);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task GetFeatureAvailabilityMappingAsync_WithValidRequest_ReturnsMapping()
        {
            // Arrange & Act
            var result = await _detector.GetFeatureAvailabilityMappingAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.FeatureMap);
            Assert.NotNull(result.PlatformFeatures);
            Assert.True(result.LastUpdated > DateTime.MinValue);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task DetectPlatformCapabilitiesAsync_WithWindowsPlatform_ReturnsCapabilities()
        {
            // Arrange & Act
            var result = await _detector.DetectPlatformCapabilitiesAsync(PlatformType.Windows);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.Windows, result.PlatformType);
            Assert.NotNull(result.Capabilities);
            Assert.True(result.Capabilities.Count > 0);
            Assert.NotNull(result.Limitations);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task DetectPlatformCapabilitiesAsync_WithMacOSPlatform_ReturnsCapabilities()
        {
            // Arrange & Act
            var result = await _detector.DetectPlatformCapabilitiesAsync(PlatformType.MacOS);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.MacOS, result.PlatformType);
            Assert.NotNull(result.Capabilities);
            Assert.True(result.Capabilities.Count > 0);
            Assert.NotNull(result.Limitations);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task DetectPlatformCapabilitiesAsync_WithLinuxPlatform_ReturnsCapabilities()
        {
            // Arrange & Act
            var result = await _detector.DetectPlatformCapabilitiesAsync(PlatformType.Linux);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.Linux, result.PlatformType);
            Assert.NotNull(result.Capabilities);
            Assert.True(result.Capabilities.Count > 0);
            Assert.NotNull(result.Limitations);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task GetFallbackStrategyAsync_WithUnavailableFeature_ReturnsFallbackOptions()
        {
            // Arrange
            var featureName = "UnavailableFeature";
            var platform = PlatformType.Windows;

            // Act
            var result = await _detector.GetFallbackStrategyAsync(featureName, platform);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(featureName, result.FeatureName);
            Assert.Equal(platform, result.TargetPlatform);
            Assert.NotNull(result.FallbackOptions);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task ValidateFeatureCompatibilityAsync_WithValidFeatures_ReturnsCompatibilityResult()
        {
            // Arrange
            var features = new List<string> { "FileSystem", "NetworkAccess" };
            var platforms = new List<PlatformType> { PlatformType.Windows, PlatformType.MacOS, PlatformType.Linux };

            // Act
            var result = await _detector.ValidateFeatureCompatibilityAsync(features, platforms);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(features.Count, result.Features.Count);
            Assert.Equal(platforms.Count, result.Platforms.Count);
            Assert.NotNull(result.CompatibilityMatrix);
            Assert.NotNull(result.Issues);
            Assert.NotNull(result.Recommendations);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task GetRecommendedFeaturesAsync_WithWindowsPlatform_ReturnsRecommendations()
        {
            // Arrange & Act
            var result = await _detector.GetRecommendedFeaturesAsync(PlatformType.Windows);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.Windows, result.PlatformType);
            Assert.NotNull(result.RecommendedFeatures);
            Assert.NotNull(result.AvoidedFeatures);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task GetRecommendedFeaturesAsync_WithMacOSPlatform_ReturnsRecommendations()
        {
            // Arrange & Act
            var result = await _detector.GetRecommendedFeaturesAsync(PlatformType.MacOS);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.MacOS, result.PlatformType);
            Assert.NotNull(result.RecommendedFeatures);
            Assert.NotNull(result.AvoidedFeatures);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task GetRecommendedFeaturesAsync_WithLinuxPlatform_ReturnsRecommendations()
        {
            // Arrange & Act
            var result = await _detector.GetRecommendedFeaturesAsync(PlatformType.Linux);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(PlatformType.Linux, result.PlatformType);
            Assert.NotNull(result.RecommendedFeatures);
            Assert.NotNull(result.AvoidedFeatures);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task MonitorFeatureChangesAsync_WithValidRequest_ReturnsMonitoringResult()
        {
            // Arrange & Act
            var result = await _detector.MonitorFeatureChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Changes);
            Assert.True(result.MonitoringTime > DateTime.MinValue);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public async Task RefreshFeatureCacheAsync_WithValidRequest_ReturnsCacheRefreshResult()
        {
            // Arrange & Act
            var result = await _detector.RefreshFeatureCacheAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RefreshTime > DateTime.MinValue);
            Assert.NotNull(result.Metadata);
        }
    }
}
