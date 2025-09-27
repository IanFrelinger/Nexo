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

namespace Nexo.Feature.Platform.Tests.Services.Performance
{
    /// <summary>
    /// Tests for performance requirements in Platform Feature Detector
    /// </summary>
    public class PerformanceTests
    {
        private readonly Mock<ILogger<PlatformFeatureDetector>> _mockLogger;
        private readonly PlatformFeatureDetector _detector;

        public PerformanceTests()
        {
            _mockLogger = new Mock<ILogger<PlatformFeatureDetector>>();
            _detector = new PlatformFeatureDetector(_mockLogger.Object);
        }

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
    }
}
