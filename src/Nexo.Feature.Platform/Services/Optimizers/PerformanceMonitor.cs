using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Services.Optimizers;

/// <summary>
/// Handles performance monitoring and metrics collection
/// </summary>
public partial class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly List<PerformanceMetricsResult> _metricsHistory;
    private bool _isMonitoring;
    private Timer? _monitoringTimer;
    private PerformanceMonitoringConfig? _monitoringConfig;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsHistory = new List<PerformanceMetricsResult>();
        _isMonitoring = false;
    }

    public async Task<PerformanceMonitoringResult> StartPerformanceMonitoringAsync(PerformanceMonitoringConfig monitoringConfig, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting performance monitoring with config: {ConfigName}", monitoringConfig.Name);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_isMonitoring)
            {
                return new PerformanceMonitoringResult
                {
                    Success = false,
                    Message = "Performance monitoring is already running",
                    StartedAt = DateTime.UtcNow
                };
            }

            _monitoringConfig = monitoringConfig;
            _isMonitoring = true;

            // Start monitoring timer
            _monitoringTimer = new Timer(async _ => await CollectMetricsAsync(cancellationToken), null, 
                TimeSpan.Zero, TimeSpan.FromSeconds(monitoringConfig.CollectionIntervalSeconds));

            stopwatch.Stop();
            var result = new PerformanceMonitoringResult
            {
                Success = true,
                Message = $"Performance monitoring started successfully in {stopwatch.ElapsedMilliseconds}ms",
                StartedAt = DateTime.UtcNow,
                ProcessingTime = stopwatch.Elapsed
            };

            _logger.LogInformation("Performance monitoring started successfully");
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Performance monitoring start was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting performance monitoring");
            return new PerformanceMonitoringResult
            {
                Success = false,
                Message = $"Performance monitoring start failed: {ex.Message}",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<PerformanceMonitoringResult> StopPerformanceMonitoringAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Stopping performance monitoring");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_isMonitoring)
            {
                return new PerformanceMonitoringResult
                {
                    Success = false,
                    Message = "Performance monitoring is not running",
                    StoppedAt = DateTime.UtcNow
                };
            }

            _isMonitoring = false;
            _monitoringTimer?.Dispose();
            _monitoringTimer = null;

            stopwatch.Stop();
            var result = new PerformanceMonitoringResult
            {
                Success = true,
                Message = $"Performance monitoring stopped successfully in {stopwatch.ElapsedMilliseconds}ms",
                StoppedAt = DateTime.UtcNow,
                ProcessingTime = stopwatch.Elapsed
            };

            _logger.LogInformation("Performance monitoring stopped successfully");
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Performance monitoring stop was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping performance monitoring");
            return new PerformanceMonitoringResult
            {
                Success = false,
                Message = $"Performance monitoring stop failed: {ex.Message}",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<PerformanceMetricsResult> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting current performance metrics");

            var metrics = await CollectCurrentMetricsAsync(cancellationToken);
            
            // Add to history
            _metricsHistory.Add(metrics);

            // Keep only recent history
            if (_metricsHistory.Count > 100)
            {
                _metricsHistory.RemoveAt(0);
            }

            _logger.LogInformation("Performance metrics collected successfully");
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return new PerformanceMetricsResult
            {
                Success = false,
                Message = $"Performance metrics collection failed: {ex.Message}",
                CollectedAt = DateTime.UtcNow
            };
        }
    }

    private async Task CollectMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!_isMonitoring || _monitoringConfig == null)
                return;

            var metrics = await CollectCurrentMetricsAsync(cancellationToken);
            _metricsHistory.Add(metrics);

            _logger.LogDebug("Performance metrics collected: CPU={CPU}%, Memory={Memory}MB", 
                metrics.CPUUsage, metrics.MemoryUsage / 1024 / 1024);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting performance metrics");
        }
    }

    private async Task<PerformanceMetricsResult> CollectCurrentMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(100, cancellationToken); // Simulate collection time

            // In a real implementation, this would collect actual performance metrics
            var random = new Random();
            var metrics = new PerformanceMetricsResult
            {
                Success = true,
                CollectedAt = DateTime.UtcNow,
                CPUUsage = random.Next(10, 90), // Simulate 10-90% CPU usage
                MemoryUsage = random.Next(1000000, 10000000), // Simulate 1-10 MB memory usage
                DiskUsage = random.Next(1000000, 50000000), // Simulate 1-50 MB disk usage
                NetworkUsage = random.Next(1000, 100000), // Simulate 1-100 KB network usage
                Message = "Performance metrics collected successfully"
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting current performance metrics");
            return new PerformanceMetricsResult
            {
                Success = false,
                Message = $"Performance metrics collection failed: {ex.Message}",
                CollectedAt = DateTime.UtcNow
            };
        }
    }

    public List<PerformanceMetricsResult> GetMetricsHistory()
    {
        return _metricsHistory.ToList();
    }

    public bool IsMonitoring => _isMonitoring;
}
