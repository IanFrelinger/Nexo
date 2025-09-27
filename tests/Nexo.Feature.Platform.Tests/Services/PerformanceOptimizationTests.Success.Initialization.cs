using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Nexo.Feature.Platform.Tests.Services;

/// <summary>
/// Success tests for PerformanceOptimization initialization functionality
/// </summary>
public partial class PerformanceOptimizationTests
{
    [Fact(Timeout = 10000)]
    public async Task InitializeAsync_WithValidPlatform_ReturnsSuccessResult()
    {
        _logger.LogInformation("Starting PerformanceOptimization initialization test");
        
        // Arrange
        var platformType = PlatformType.Windows;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _performanceOptimization.InitializeAsync(platformType, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.Equal(platformType, result.PlatformType);
        Assert.NotNull(result.AvailableOptimizations);
        Assert.True(result.AvailableOptimizations.Count > 0, "Should have available optimizations");
        Assert.True(result.InitializationTime > DateTime.UtcNow.AddMinutes(-1), "Initialization time should be recent");
        
        _logger.LogInformation("PerformanceOptimization initialization test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public async Task InitializeAsync_WithDifferentPlatforms_ReturnsSuccessResult()
    {
        _logger.LogInformation("Starting multi-platform initialization test");
        
        var platforms = new[] { PlatformType.Windows, PlatformType.MacOS, PlatformType.Linux };
        
        foreach (var platform in platforms)
        {
            // Arrange
            var optimization = new PerformanceOptimization(NullLogger<PerformanceOptimization>.Instance);
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await optimization.InitializeAsync(platform, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess, $"Expected IsSuccess=true for {platform}, got {result.IsSuccess}");
            Assert.Equal(platform, result.PlatformType);
            Assert.NotNull(result.AvailableOptimizations);
            
            _logger.LogInformation("Platform {Platform} initialization successful", platform);
        }
        
        _logger.LogInformation("Multi-platform initialization test completed successfully");
    }
}
