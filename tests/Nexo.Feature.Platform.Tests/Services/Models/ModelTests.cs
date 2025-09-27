using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;
using Xunit;

namespace Nexo.Feature.Platform.Tests.Services.Models
{
    /// <summary>
    /// Tests for data models and interface implementation in Platform Feature Detector
    /// </summary>
    public class ModelTests
    {
        private readonly Mock<ILogger<PlatformFeatureDetector>> _mockLogger;
        private readonly PlatformFeatureDetector _detector;

        public ModelTests()
        {
            _mockLogger = new Mock<ILogger<PlatformFeatureDetector>>();
            _detector = new PlatformFeatureDetector(_mockLogger.Object);
        }

        [Fact]
        public void IPlatformFeatureDetector_Interface_IsDefined()
        {
            // Arrange & Act
            var detector = _detector as IPlatformFeatureDetector;

            // Assert
            Assert.NotNull(detector);
        }

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
    }
}
