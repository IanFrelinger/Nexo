using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;

namespace Nexo.Feature.Platform.Tests.Services.NativeAPIIntegration
{
    /// <summary>
    /// Tests for Native API Integration initialization.
    /// </summary>
    public class InitializationTests
    {
        private readonly NativeAPIIntegration _integration;
        private readonly Mock<ILogger<NativeAPIIntegration>> _mockLogger;

        public InitializationTests(NativeAPIIntegration integration, Mock<ILogger<NativeAPIIntegration>> mockLogger)
        {
            _integration = integration ?? throw new ArgumentNullException(nameof(integration));
            _mockLogger = mockLogger ?? throw new ArgumentNullException(nameof(mockLogger));
        }

        [Fact]
        public async Task InitializeAsync_WithValidParameters_ReturnsSuccess()
        {
            // Arrange
            var platform = PlatformType.Android;
            var capabilities = new List<NativeAPICapability>
            {
                new NativeAPICapability { Name = "TestCapability", IsSupported = true }
            };

            // Act
            var result = await _integration.InitializeAsync(platform, capabilities);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotEmpty(result.Message);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Capabilities);
            Assert.True(result.InitializationTime > DateTime.MinValue);
        }

        [Fact]
        public async Task InitializeAsync_WithInvalidParameters_ReturnsFailure()
        {
            // Arrange
            var platform = PlatformType.Unknown;
            var capabilities = new List<NativeAPICapability>();

            // Act
            var result = await _integration.InitializeAsync(platform, capabilities);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.NotNull(result.Capabilities);
        }

        [Fact]
        public async Task InitializeAsync_WithNullParameters_ThrowsArgumentNullException()
        {
            // Arrange
            var platform = PlatformType.Android;
            List<NativeAPICapability> capabilities = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _integration.InitializeAsync(platform, capabilities));
        }

        [Fact]
        public async Task InitializeAsync_WithEmptyParameters_ReturnsFailure()
        {
            // Arrange
            var platform = PlatformType.Android;
            var capabilities = new List<NativeAPICapability>();

            // Act
            var result = await _integration.InitializeAsync(platform, capabilities);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.NotNull(result.Capabilities);
        }

        [Fact]
        public async Task InitializeAsync_WithUnsupportedPlatform_ReturnsFailure()
        {
            // Arrange
            var platform = PlatformType.Unknown;
            var capabilities = new List<NativeAPICapability>
            {
                new NativeAPICapability { Name = "TestCapability", IsSupported = true }
            };

            // Act
            var result = await _integration.InitializeAsync(platform, capabilities);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.NotNull(result.Capabilities);
        }

        [Fact]
        public async Task InitializeAsync_WithCancellationToken_CancelsCorrectly()
        {
            // Arrange
            var platform = PlatformType.Android;
            var capabilities = new List<NativeAPICapability>
            {
                new NativeAPICapability { Name = "TestCapability", IsSupported = true }
            };
            var cancellationToken = new CancellationToken(true);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _integration.InitializeAsync(platform, capabilities, cancellationToken));
        }

        [Fact]
        public async Task InitializeAsync_WithValidCapabilities_InitializesCorrectly()
        {
            // Arrange
            var platform = PlatformType.Android;
            var capabilities = new List<NativeAPICapability>
            {
                new NativeAPICapability 
                { 
                    Name = "TestCapability1", 
                    IsSupported = true,
                    Description = "Test Description 1",
                    Version = "1.0.0"
                },
                new NativeAPICapability 
                { 
                    Name = "TestCapability2", 
                    IsSupported = false,
                    Description = "Test Description 2",
                    Version = "2.0.0"
                }
            };

            // Act
            var result = await _integration.InitializeAsync(platform, capabilities);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotEmpty(result.Message);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Capabilities);
            Assert.Equal(2, result.Capabilities.Count);
            Assert.True(result.InitializationTime > DateTime.MinValue);
        }

        [Fact]
        public async Task InitializeAsync_WithMixedCapabilities_HandlesCorrectly()
        {
            // Arrange
            var platform = PlatformType.Android;
            var capabilities = new List<NativeAPICapability>
            {
                new NativeAPICapability { Name = "SupportedCapability", IsSupported = true },
                new NativeAPICapability { Name = "UnsupportedCapability", IsSupported = false }
            };

            // Act
            var result = await _integration.InitializeAsync(platform, capabilities);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotEmpty(result.Message);
            Assert.NotNull(result.Capabilities);
            Assert.Equal(2, result.Capabilities.Count);
            Assert.True(result.InitializationTime > DateTime.MinValue);
        }

        [Fact]
        public async Task InitializeAsync_WithDuplicateCapabilities_HandlesCorrectly()
        {
            // Arrange
            var platform = PlatformType.Android;
            var capabilities = new List<NativeAPICapability>
            {
                new NativeAPICapability { Name = "DuplicateCapability", IsSupported = true },
                new NativeAPICapability { Name = "DuplicateCapability", IsSupported = false }
            };

            // Act
            var result = await _integration.InitializeAsync(platform, capabilities);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotEmpty(result.Message);
            Assert.NotNull(result.Capabilities);
            Assert.Equal(2, result.Capabilities.Count);
            Assert.True(result.InitializationTime > DateTime.MinValue);
        }
    }
}
