using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Unity.Interfaces;
using Nexo.Feature.Unity.Models;
using Nexo.Core.Application.Services.Adaptation;

namespace Nexo.Feature.Unity.Monitoring
{
    /// <summary>
    /// Real-time game performance monitoring with automatic optimization
    /// </summary>
    public partial class GamePerformanceMonitor : IGamePerformanceMonitor
    {
        private readonly IUnityProfilerIntegration _profilerIntegration;
        private readonly IAdaptationEngine _adaptationEngine;
        private readonly IPerformanceAnalyzer _performanceAnalyzer;
        private readonly ILogger<GamePerformanceMonitor> _logger;
        
        private readonly Dictionary<string, PerformanceThreshold> _performanceThresholds;
        private readonly ConcurrentQueue<GamePerformanceSnapshot> _performanceHistory;
        private readonly Timer _monitoringTimer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        public GamePerformanceMonitor(
            IUnityProfilerIntegration profilerIntegration,
            IAdaptationEngine adaptationEngine,
            IPerformanceAnalyzer performanceAnalyzer,
            ILogger<GamePerformanceMonitor> logger)
        {
            _profilerIntegration = profilerIntegration;
            _adaptationEngine = adaptationEngine;
            _performanceAnalyzer = performanceAnalyzer;
            _logger = logger;
            
            _performanceThresholds = new Dictionary<string, PerformanceThreshold>();
            _performanceHistory = new ConcurrentQueue<GamePerformanceSnapshot>();
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Initialize monitoring timer
            _monitoringTimer = new Timer(MonitoringTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }
        
        public async Task StartMonitoringAsync(GameMonitoringConfiguration config)
        {
            _logger.LogInformation("Starting game performance monitoring for {GameName}", config.GameName);
            
            try
            {
                // Configure performance thresholds
                ConfigurePerformanceThresholds(config);
                
                // Start Unity profiler integration
                await _profilerIntegration.StartProfilingAsync(config.ProfilingConfiguration);
                
                // Begin continuous monitoring loop
                _ = Task.Run(() => ContinuousMonitoringLoop(config), config.CancellationToken);
                
                _logger.LogInformation("Game performance monitoring started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start game performance monitoring");
                throw;
            }
        }
        
        public async Task StopMonitoringAsync()
        {
            _logger.LogInformation("Stopping game performance monitoring");
            
            try
            {
                // Stop monitoring timer
                _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
                
                // Cancel monitoring tasks
                _cancellationTokenSource.Cancel();
                
                // Stop Unity profiler integration
                await _profilerIntegration.StopProfilingAsync();
                
                _logger.LogInformation("Game performance monitoring stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop game performance monitoring");
                throw;
            }
        }
        
        public async Task<GamePerformanceReport> GeneratePerformanceReportAsync(TimeSpan timeRange)
        {
            _logger.LogInformation("Generating performance report for time range: {TimeRange}", timeRange);
            
            try
            {
                var cutoffTime = DateTime.UtcNow - timeRange;
                var relevantSnapshots = _performanceHistory
                    .Where(s => s.Timestamp >= cutoffTime)
                    .ToList();
                
                if (!relevantSnapshots.Any())
                {
                    _logger.LogWarning("No performance data available for the specified time range");
                    return GamePerformanceReport.Empty;
                }
                
                var report = new GamePerformanceReport
                {
                    TimeRange = timeRange,
                    SnapshotCount = relevantSnapshots.Count,
                    
                    // Frame rate statistics
                    AverageFrameRate = relevantSnapshots.Average(s => s.FrameRate),
                    MinFrameRate = relevantSnapshots.Min(s => s.FrameRate),
                    MaxFrameRate = relevantSnapshots.Max(s => s.FrameRate),
                    FrameRateStandardDeviation = CalculateStandardDeviation(relevantSnapshots.Select(s => s.FrameRate)),
                    
                    // Performance trends
                    PerformanceTrends = AnalyzePerformanceTrends(relevantSnapshots),
                    
                    // Critical performance events
                    CriticalEvents = IdentifyCriticalEvents(relevantSnapshots),
                    
                    // Optimization opportunities
                    OptimizationOpportunities = await IdentifyOptimizationOpportunities(relevantSnapshots),
                    
                    // Platform-specific insights
                    PlatformInsights = GeneratePlatformInsights(relevantSnapshots)
                };
                
                _logger.LogInformation("Performance report generated successfully with {SnapshotCount} snapshots", relevantSnapshots.Count);
                
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate performance report");
                throw;
            }
        }
        
        public async Task<GamePerformanceSnapshot> GetCurrentPerformanceSnapshotAsync()
        {
            try
            {
                return await CapturePerformanceSnapshot();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture current performance snapshot");
                throw;
            }
        }
        
        
        public void Dispose()
        {
            _monitoringTimer?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}
