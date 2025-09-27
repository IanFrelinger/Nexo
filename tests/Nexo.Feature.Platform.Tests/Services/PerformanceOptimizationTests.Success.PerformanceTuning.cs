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
/// Success tests for PerformanceOptimization performance tuning functionality
/// </summary>
public partial class PerformanceOptimizationTests
{
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
}
