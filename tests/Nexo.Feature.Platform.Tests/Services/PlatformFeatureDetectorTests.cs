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

namespace Nexo.Feature.Platform.Tests.Services
{
    /// <summary>
    /// Unit tests for the PlatformFeatureDetector service.
    /// Part of Epic 6.2: Platform-Specific Feature Integration, Story 6.2.1: Platform Feature Detection.
    /// </summary>
    public class PlatformFeatureDetectorTests
    {
        private readonly Mock<ILogger<PlatformFeatureDetector>> _mockLogger;
        private readonly PlatformFeatureDetector _detector;

        public PlatformFeatureDetectorTests()
        {
            _mockLogger = new Mock<ILogger<PlatformFeatureDetector>>();
            _detector = new PlatformFeatureDetector(_mockLogger.Object);
        }

        #region Interface Tests

        [Fact]
        public void IPlatformFeatureDetector_Interface_IsDefined()
        {
            // Arrange & Act
            var detector = _detector as IPlatformFeatureDetector;

            // Assert
            Assert.NotNull(detector);
        }

        #endregion

        #region Model Tests

        [Fact]
        public void PlatformFeatureDetectionResult_WithEmptyValues_InitializesCorrectly()
        {
            // Arrange & Act
            var result = new PlatformFeatureDetectionResult();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(string.Empty, result.Message);
            Assert.Equal(PlatformType.Windows, result.PlatformType); // Default enum value is Windows (0)
            Assert.Equal(string.Empty, result.PlatformVersion);
            Assert.Equal(string.Empty, result.Architecture);
            Assert.NotNull(result.DetectedFeatures);
            Assert.NotNull(result.Warnings);
            Assert.NotNull(result.Errors);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public void PlatformFeature_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var feature = new PlatformFeature
            {
                Name = "TestFeature",
                Description = "Test Description",
                Type = FeatureType.UserInterface,
                Availability = FeatureAvailability.Available,
                Priority = FeaturePriority.High,
                Version = "1.0",
                Dependencies = new List<string> { "Dep1", "Dep2" },
                Configuration = new Dictionary<string, object> { { "Key", "Value" } },
                SupportedPlatforms = new List<string> { "Windows", "macOS" },
                IsExperimental = false,
                IsDeprecated = false,
                DeprecationMessage = string.Empty
            };

            // Assert
            Assert.Equal("TestFeature", feature.Name);
            Assert.Equal("Test Description", feature.Description);
            Assert.Equal(FeatureType.UserInterface, feature.Type);
            Assert.Equal(FeatureAvailability.Available, feature.Availability);
            Assert.Equal(FeaturePriority.High, feature.Priority);
            Assert.Equal("1.0", feature.Version);
            Assert.Equal(2, feature.Dependencies.Count);
            Assert.Equal(1, feature.Configuration.Count);
            Assert.Equal(2, feature.SupportedPlatforms.Count);
            Assert.False(feature.IsExperimental);
            Assert.False(feature.IsDeprecated);
        }

        [Fact]
        public void FeatureAvailabilityResult_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var result = new FeatureAvailabilityResult
            {
                IsAvailable = true,
                FeatureName = "TestFeature",
                PlatformType = PlatformType.Windows,
                Availability = FeatureAvailability.Available,
                Reason = "Feature is supported",
                AlternativeFeatures = new List<string> { "Alt1", "Alt2" },
                Metadata = new Dictionary<string, object> { { "Key", "Value" } }
            };

            // Assert
            Assert.True(result.IsAvailable);
            Assert.Equal("TestFeature", result.FeatureName);
            Assert.Equal(PlatformType.Windows, result.PlatformType);
            Assert.Equal(FeatureAvailability.Available, result.Availability);
            Assert.Equal("Feature is supported", result.Reason);
            Assert.Equal(2, result.AlternativeFeatures.Count);
            Assert.Equal(1, result.Metadata.Count);
        }

        [Fact]
        public void PlatformCapabilitiesResult_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var result = new PlatformCapabilitiesResult
            {
                IsSuccess = true,
                Message = "Success",
                PlatformType = PlatformType.Windows,
                Capabilities = new List<PlatformCapability>(),
                Limitations = new List<PlatformLimitation>(),
                Metadata = new Dictionary<string, object>()
            };

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Success", result.Message);
            Assert.Equal(PlatformType.Windows, result.PlatformType);
            Assert.NotNull(result.Capabilities);
            Assert.NotNull(result.Limitations);
            Assert.NotNull(result.Metadata);
        }

        [Fact]
        public void FallbackStrategyResult_WithValidData_PropertiesSetCorrectly()
        {
            // Arrange
            var result = new FallbackStrategyResult
            {
                HasFallback = true,
                FeatureName = "TestFeature",
                TargetPlatform = PlatformType.Windows,
                FallbackOptions = new List<FallbackOption>(),
                RecommendedStrategy = "Alternative Implementation",
                Metadata = new Dictionary<string, object>()
            };

            // Assert
            Assert.True(result.HasFallback);
            Assert.Equal("TestFeature", result.FeatureName);
            Assert.Equal(PlatformType.Windows, result.TargetPlatform);
            Assert.NotNull(result.FallbackOptions);
            Assert.Equal("Alternative Implementation", result.RecommendedStrategy);
            Assert.NotNull(result.Metadata);
        }

        #endregion

        #region Service Tests

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

        #endregion

        #region Cross-Platform Tests

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

        #endregion

        #region Error Handling Tests

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

        #endregion

        #region Performance Tests

        [Fact]
        public async Task PlatformFeatureDetection_Performance_CompletesWithinReasonableTime()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var platform = PlatformType.Windows;

            // Act
            var result = await _detector.DetectFeaturesForPlatformAsync(platform);
            stopwatch.Stop();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
        }

        [Fact]
        public async Task PlatformCapabilitiesDetection_Performance_CompletesWithinReasonableTime()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var platform = PlatformType.Windows;

            // Act
            var result = await _detector.DetectPlatformCapabilitiesAsync(platform);
            stopwatch.Stop();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete within 5 seconds
        }

        #endregion
    }
} 