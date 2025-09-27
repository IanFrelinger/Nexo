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
/// Success tests for PerformanceOptimization analysis functionality
/// </summary>
public partial class PerformanceOptimizationTests
{
    [Fact(Timeout = 10000)]
    public async Task AnalyzePerformanceAsync_ReturnsValidAnalysis()
    {
        _logger.LogInformation("Starting performance analysis test");
        
        // Arrange
        var platformType = PlatformType.Linux;
        await _performanceOptimization.InitializeAsync(platformType);

        // Act
        var result = await _performanceOptimization.AnalyzePerformanceAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.NotNull(result.Bottlenecks);
        Assert.NotNull(result.Recommendations);
        Assert.True(result.OverallPerformanceScore >= 0 && result.OverallPerformanceScore <= 100, "Performance score should be between 0-100");
        Assert.True(result.AnalysisTime > DateTime.UtcNow.AddMinutes(-1), "Analysis time should be recent");
        
        _logger.LogInformation("Performance analysis test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public async Task GetPerformanceRecommendationsAsync_ReturnsValidRecommendations()
    {
        _logger.LogInformation("Starting performance recommendations test");
        
        // Arrange
        var platformType = PlatformType.Windows;
        await _performanceOptimization.InitializeAsync(platformType);

        // Act
        var result = await _performanceOptimization.GetPerformanceRecommendationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.Equal(platformType, result.PlatformType);
        Assert.NotNull(result.Recommendations);
        
        _logger.LogInformation("Performance recommendations test completed successfully");
    }
}
