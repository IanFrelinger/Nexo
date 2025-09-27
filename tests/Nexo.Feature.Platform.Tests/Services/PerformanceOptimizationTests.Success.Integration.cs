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
/// Success tests for PerformanceOptimization integration and lifecycle functionality
/// </summary>
public partial class PerformanceOptimizationTests
{
    [Fact(Timeout = 10000)]
    public async Task DisposeAsync_ReturnsSuccessResult()
    {
        _logger.LogInformation("Starting dispose test");
        
        // Arrange
        var platformType = PlatformType.MacOS;
        await _performanceOptimization.InitializeAsync(platformType);

        // Act
        var result = await _performanceOptimization.DisposeAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.True(result.DisposedResources >= 0, "Disposed resources should be non-negative");
        Assert.NotNull(result.DisposedComponents);
        
        _logger.LogInformation("Dispose test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public async Task PerformanceOptimization_Integration_WorksEndToEnd()
    {
        _logger.LogInformation("Starting end-to-end integration test");
        
        // Arrange
        var platformType = PlatformType.Linux;
        var cancellationToken = CancellationToken.None;

        // Act & Assert - Initialize
        var initResult = await _performanceOptimization.InitializeAsync(platformType, cancellationToken);
        Assert.True(initResult.IsSuccess, "Initialization should succeed");

        // Act & Assert - Get metrics
        var metricsResult = await _performanceOptimization.GetPerformanceMetricsAsync(cancellationToken);
        Assert.True(metricsResult.IsSuccess, "Getting metrics should succeed");

        // Act & Assert - Analyze performance
        var analysisResult = await _performanceOptimization.AnalyzePerformanceAsync(cancellationToken);
        Assert.True(analysisResult.IsSuccess, "Performance analysis should succeed");

        // Act & Assert - Get recommendations
        var recommendationsResult = await _performanceOptimization.GetPerformanceRecommendationsAsync(cancellationToken);
        Assert.True(recommendationsResult.IsSuccess, "Getting recommendations should succeed");

        // Act & Assert - Apply automatic optimizations
        var autoOptimizationResult = await _performanceOptimization.ApplyAutomaticOptimizationsAsync(cancellationToken);
        Assert.True(autoOptimizationResult.IsSuccess, "Automatic optimizations should succeed");

        // Act & Assert - Dispose
        var disposeResult = await _performanceOptimization.DisposeAsync(cancellationToken);
        Assert.True(disposeResult.IsSuccess, "Disposal should succeed");
        
        _logger.LogInformation("End-to-end integration test completed successfully");
    }
}
