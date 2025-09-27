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
/// Success tests for PerformanceOptimization monitoring functionality
/// </summary>
public partial class PerformanceOptimizationTests
{
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
}
