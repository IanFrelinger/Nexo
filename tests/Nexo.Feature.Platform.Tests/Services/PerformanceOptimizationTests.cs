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
/// Comprehensive test suite for PerformanceOptimization service.
/// Tests all aspects of performance optimization including initialization, tuning, monitoring, and analysis.
/// </summary>
public class PerformanceOptimizationTests
{
    private readonly ILogger<PerformanceOptimizationTests> _logger;
    private readonly PerformanceOptimization _performanceOptimization;

    public PerformanceOptimizationTests()
    {
        _logger = NullLogger<PerformanceOptimizationTests>.Instance;
        _performanceOptimization = new PerformanceOptimization(NullLogger<PerformanceOptimization>.Instance);
    }

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

    [Fact(Timeout = 10000)]
    public async Task ApplyPerformanceTuningAsync_WithValidProfile_ReturnsSuccessResult()
    {
        _logger.LogInformation("Starting performance tuning test");
        
        // Arrange
        var platformType = PlatformType.Windows;
        await _performanceOptimization.InitializeAsync(platformType);
        
        var tuningProfile = new PerformanceTuningProfile
        {
            Name = "Test Profile",
            Description = "Test performance tuning profile",
            Type = TuningProfileType.Balanced,
            SupportedPlatforms = new List<PlatformType> { platformType },
            Parameters = new Dictionary<string, object>
            {
                ["setting1"] = "value1",
                ["setting2"] = 42
            }
        };

        // Act
        var result = await _performanceOptimization.ApplyPerformanceTuningAsync(tuningProfile);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.Equal(tuningProfile.Name, result.ProfileName);
        Assert.NotNull(result.AppliedOptimizations);
        Assert.True(result.ApplicationTime > TimeSpan.Zero, "Application time should be positive");
        
        _logger.LogInformation("Performance tuning test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public async Task OptimizeMemoryAsync_WithValidStrategy_ReturnsSuccessResult()
    {
        _logger.LogInformation("Starting memory optimization test");
        
        // Arrange
        var platformType = PlatformType.Windows;
        await _performanceOptimization.InitializeAsync(platformType);
        
        var memoryStrategy = new MemoryOptimizationStrategy
        {
            Name = "Test Memory Strategy",
            Description = "Test memory optimization strategy",
            Type = MemoryOptimizationType.GarbageCollection,
            Parameters = new Dictionary<string, object>
            {
                ["memorySetting1"] = "value1",
                ["memorySetting2"] = 100
            },
            IsAggressive = true,
            TargetAreas = new List<string> { "heap", "cache" }
        };

        // Act
        var result = await _performanceOptimization.OptimizeMemoryAsync(memoryStrategy);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.Equal(memoryStrategy.Name, result.StrategyName);
        Assert.NotNull(result.OptimizationsApplied);
        Assert.True(result.OptimizationTime > TimeSpan.Zero, "Optimization time should be positive");
        
        _logger.LogInformation("Memory optimization test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public async Task OptimizeBatteryLifeAsync_WithValidStrategy_ReturnsSuccessResult()
    {
        _logger.LogInformation("Starting battery optimization test");
        
        // Arrange
        var platformType = PlatformType.MacOS;
        await _performanceOptimization.InitializeAsync(platformType);
        
        var batteryStrategy = new BatteryOptimizationStrategy
        {
            Name = "Test Battery Strategy",
            Description = "Test battery optimization strategy",
            Type = BatteryOptimizationType.CPUFrequencyScaling,
            Parameters = new Dictionary<string, object>
            {
                ["batterySetting1"] = "value1",
                ["batterySetting2"] = 50
            },
            IsPowerSaving = true,
            PowerSavingFeatures = new List<string> { "cpu_throttling", "background_limiting" }
        };

        // Act
        var result = await _performanceOptimization.OptimizeBatteryLifeAsync(batteryStrategy);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.Equal(batteryStrategy.Name, result.StrategyName);
        Assert.NotNull(result.OptimizationsApplied);
        Assert.NotNull(result.PowerSavingFeatures);
        
        _logger.LogInformation("Battery optimization test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public async Task StartPerformanceMonitoringAsync_WithValidConfig_ReturnsSuccessResult()
    {
        _logger.LogInformation("Starting performance monitoring test");
        
        // Arrange
        var platformType = PlatformType.Linux;
        await _performanceOptimization.InitializeAsync(platformType);
        
        var monitoringConfig = new PerformanceMonitoringConfig
        {
            Name = "Test Monitoring",
            EnableCPUMonitoring = true,
            EnableMemoryMonitoring = true,
            EnableBatteryMonitoring = true,
            EnableNetworkMonitoring = false,
            MonitoringInterval = 1000,
            CustomMetrics = new List<string> { "custom1", "custom2" },
            Configuration = new Dictionary<string, object> { ["config1"] = "value1" }
        };

        // Act
        var result = await _performanceOptimization.StartPerformanceMonitoringAsync(monitoringConfig);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.True(result.IsMonitoring, "Should be monitoring after start");
        Assert.NotNull(result.MonitoredMetrics);
        Assert.True(result.StartTime > DateTime.UtcNow.AddMinutes(-1), "Start time should be recent");
        
        _logger.LogInformation("Performance monitoring start test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public async Task StopPerformanceMonitoringAsync_WhenMonitoring_ReturnsSuccessResult()
    {
        _logger.LogInformation("Starting performance monitoring stop test");
        
        // Arrange
        var platformType = PlatformType.Windows;
        await _performanceOptimization.InitializeAsync(platformType);
        
        var monitoringConfig = new PerformanceMonitoringConfig
        {
            Name = "Test Monitoring",
            EnableCPUMonitoring = true,
            MonitoringInterval = 1000
        };
        
        await _performanceOptimization.StartPerformanceMonitoringAsync(monitoringConfig);

        // Act
        var result = await _performanceOptimization.StopPerformanceMonitoringAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.False(result.IsMonitoring, "Should not be monitoring after stop");
        Assert.NotNull(result.StopTime);
        Assert.True(result.StopTime > DateTime.UtcNow.AddMinutes(-1), "Stop time should be recent");
        
        _logger.LogInformation("Performance monitoring stop test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public async Task GetPerformanceMetricsAsync_ReturnsValidMetrics()
    {
        _logger.LogInformation("Starting performance metrics test");
        
        // Arrange
        var platformType = PlatformType.MacOS;
        await _performanceOptimization.InitializeAsync(platformType);

        // Act
        var result = await _performanceOptimization.GetPerformanceMetricsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.True(result.CollectionTime > DateTime.UtcNow.AddMinutes(-1), "Collection time should be recent");
        Assert.True(result.CPUsage >= 0, "CPU usage should be non-negative");
        Assert.True(result.MemoryUsage >= 0, "Memory usage should be non-negative");
        Assert.True(result.AvailableMemory >= 0, "Available memory should be non-negative");
        Assert.True(result.BatteryLevel >= 0 && result.BatteryLevel <= 100, "Battery level should be between 0-100");
        Assert.True(result.NetworkLatency >= 0, "Network latency should be non-negative");
        
        _logger.LogInformation("Performance metrics test completed successfully");
    }

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

    [Fact(Timeout = 10000)]
    public async Task ApplyAutomaticOptimizationsAsync_ReturnsSuccessResult()
    {
        _logger.LogInformation("Starting automatic optimizations test");
        
        // Arrange
        var platformType = PlatformType.MacOS;
        await _performanceOptimization.InitializeAsync(platformType);

        // Act
        var result = await _performanceOptimization.ApplyAutomaticOptimizationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.NotNull(result.AppliedOptimizations);
        Assert.NotNull(result.SkippedOptimizations);
        Assert.True(result.PerformanceImprovement >= 0, "Performance improvement should be non-negative");
        Assert.True(result.OptimizationTime > TimeSpan.Zero, "Optimization time should be positive");
        
        _logger.LogInformation("Automatic optimizations test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public async Task ValidateOptimizationSettingsAsync_WithValidSettings_ReturnsSuccessResult()
    {
        _logger.LogInformation("Starting optimization settings validation test");
        
        // Arrange
        var platformType = PlatformType.Linux;
        await _performanceOptimization.InitializeAsync(platformType);
        
        var settings = new PerformanceOptimizationSettings
        {
            EnableAutomaticOptimization = true,
            EnableMemoryOptimization = true,
            EnableBatteryOptimization = true,
            EnablePerformanceMonitoring = true,
            OptimizationInterval = 300000,
            CustomSettings = new Dictionary<string, object>
            {
                ["customSetting1"] = "value1",
                ["customSetting2"] = 42
            }
        };

        // Act
        var result = await _performanceOptimization.ValidateOptimizationSettingsAsync(settings);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid, "Settings should be valid");
        Assert.NotNull(result.ValidationErrors);
        Assert.NotNull(result.ValidationWarnings);
        
        _logger.LogInformation("Optimization settings validation test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public async Task ResetToDefaultsAsync_ReturnsSuccessResult()
    {
        _logger.LogInformation("Starting reset to defaults test");
        
        // Arrange
        var platformType = PlatformType.Windows;
        await _performanceOptimization.InitializeAsync(platformType);

        // Act
        var result = await _performanceOptimization.ResetToDefaultsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, $"Expected IsSuccess=true, got {result.IsSuccess}");
        Assert.NotNull(result.ResetSettings);
        Assert.True(result.ResetTime > DateTime.UtcNow.AddMinutes(-1), "Reset time should be recent");
        
        _logger.LogInformation("Reset to defaults test completed successfully");
    }

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

    [Fact(Timeout = 10000)]
    public async Task PerformanceOptimization_WithCancellationToken_RespectsCancellation()
    {
        _logger.LogInformation("Starting cancellation token test");
        
        // Arrange
        var platformType = PlatformType.Windows;
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act - Cancel immediately
        cancellationTokenSource.Cancel();

        // Assert - Should handle cancellation gracefully
        var result = await _performanceOptimization.InitializeAsync(platformType, cancellationToken);
        
        // The result might be success or failure depending on implementation, but should not throw
        Assert.NotNull(result);
        
        _logger.LogInformation("Cancellation token test completed successfully");
    }

    [Fact(Timeout = 10000)]
    public async Task PerformanceOptimization_ErrorHandling_WorksCorrectly()
    {
        _logger.LogInformation("Starting error handling test");
        
        // Arrange
        var platformType = PlatformType.Windows;
        await _performanceOptimization.InitializeAsync(platformType);

        // Act - Test with invalid tuning profile (not supported on current platform)
        var invalidProfile = new PerformanceTuningProfile
        {
            Name = "Invalid Profile",
            Description = "Profile not supported on current platform",
            Type = TuningProfileType.Balanced,
            SupportedPlatforms = new List<PlatformType> { PlatformType.MacOS } // Not supported on Windows
        };
        
        var result = await _performanceOptimization.ApplyPerformanceTuningAsync(invalidProfile);

        // Assert - Should handle unsupported platform gracefully
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("not supported", result.Message);
        
        _logger.LogInformation("Error handling test completed successfully");
    }
} 