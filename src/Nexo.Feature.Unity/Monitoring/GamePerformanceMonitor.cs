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
    public class GamePerformanceMonitor : IGamePerformanceMonitor
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
        
        private async Task ContinuousMonitoringLoop(GameMonitoringConfiguration config)
        {
            _logger.LogInformation("Starting continuous monitoring loop");
            
            while (!config.CancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Capture current performance snapshot
                    var snapshot = await CapturePerformanceSnapshot();
                    _performanceHistory.Enqueue(snapshot);
                    
                    // Maintain history size
                    if (_performanceHistory.Count > config.MaxHistorySize)
                    {
                        _performanceHistory.TryDequeue(out _);
                    }
                    
                    // Analyze performance in real-time
                    var analysis = await _performanceAnalyzer.AnalyzeSnapshotAsync(snapshot);
                    
                    // Check for performance issues
                    await CheckPerformanceThresholds(snapshot, analysis);
                    
                    // Trigger adaptations if needed
                    if (analysis.RequiresImmediateAction)
                    {
                        await TriggerPerformanceAdaptation(snapshot, analysis);
                    }
                    
                    // Update performance dashboard
                    await UpdatePerformanceDashboard(snapshot, analysis);
                    
                    await Task.Delay(config.MonitoringInterval, config.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Monitoring loop cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in game performance monitoring loop");
                    await Task.Delay(TimeSpan.FromSeconds(1), config.CancellationToken);
                }
            }
            
            _logger.LogInformation("Continuous monitoring loop ended");
        }
        
        private async Task<GamePerformanceSnapshot> CapturePerformanceSnapshot()
        {
            var profilerData = await _profilerIntegration.GetCurrentProfilerDataAsync();
            
            return new GamePerformanceSnapshot
            {
                Timestamp = DateTime.UtcNow,
                FrameRate = profilerData.FrameRate,
                FrameTime = profilerData.FrameTime,
                CpuTime = profilerData.CpuTime,
                GpuTime = profilerData.GpuTime,
                MemoryUsage = profilerData.MemoryUsage,
                GarbageCollectionTime = profilerData.GCTime,
                DrawCalls = profilerData.DrawCalls,
                BatchedDrawCalls = profilerData.BatchedDrawCalls,
                Triangles = profilerData.TriangleCount,
                Vertices = profilerData.VertexCount,
                PlayerCount = profilerData.ActivePlayerCount,
                GameState = profilerData.CurrentGameState
            };
        }
        
        private async Task CheckPerformanceThresholds(GamePerformanceSnapshot snapshot, PerformanceAnalysis analysis)
        {
            foreach (var threshold in _performanceThresholds.Values)
            {
                var value = GetMetricValue(snapshot, threshold.MetricName);
                
                if (threshold.IsExceeded(value))
                {
                    _logger.LogWarning("Performance threshold exceeded: {MetricName} = {Value} (threshold: {Threshold})",
                        threshold.MetricName, value, threshold.ThresholdValue);
                    
                    await HandleThresholdExceeded(threshold, snapshot, analysis);
                }
            }
        }
        
        private async Task HandleThresholdExceeded(PerformanceThreshold threshold, GamePerformanceSnapshot snapshot, PerformanceAnalysis analysis)
        {
            // Log threshold exceeded event
            _logger.LogWarning("Performance threshold exceeded: {MetricName}", threshold.MetricName);
            
            // Trigger adaptation if configured
            if (threshold.TriggerAdaptation)
            {
                await TriggerPerformanceAdaptation(snapshot, analysis);
            }
            
            // Send alert if configured
            if (threshold.SendAlert)
            {
                await SendPerformanceAlert(threshold, snapshot);
            }
        }
        
        private async Task TriggerPerformanceAdaptation(GamePerformanceSnapshot snapshot, PerformanceAnalysis analysis)
        {
            var adaptationContext = new AdaptationContext
            {
                Trigger = AdaptationTrigger.GamePerformanceDegradation,
                Priority = DetermineAdaptationPriority(analysis),
                Context = new Dictionary<string, object>
                {
                    ["PerformanceSnapshot"] = snapshot,
                    ["PerformanceAnalysis"] = analysis,
                    ["GameSpecific"] = true
                }
            };
            
            await _adaptationEngine.TriggerAdaptationAsync(adaptationContext);
            
            _logger.LogInformation("Triggered performance adaptation due to {IssueType} with severity {Severity}",
                analysis.PrimaryIssue, analysis.Severity);
        }
        
        private async Task SendPerformanceAlert(PerformanceThreshold threshold, GamePerformanceSnapshot snapshot)
        {
            // Implementation would send alert to monitoring system
            _logger.LogWarning("Performance alert sent for threshold: {MetricName}", threshold.MetricName);
        }
        
        private async Task UpdatePerformanceDashboard(GamePerformanceSnapshot snapshot, PerformanceAnalysis analysis)
        {
            // Implementation would update real-time dashboard
            _logger.LogDebug("Performance dashboard updated with latest snapshot");
        }
        
        private void ConfigurePerformanceThresholds(GameMonitoringConfiguration config)
        {
            _performanceThresholds.Clear();
            
            // Frame rate threshold
            _performanceThresholds["FrameRate"] = new PerformanceThreshold
            {
                MetricName = "FrameRate",
                ThresholdValue = config.TargetFrameRate,
                ComparisonType = ComparisonType.LessThan,
                TriggerAdaptation = true,
                SendAlert = true
            };
            
            // Memory usage threshold
            _performanceThresholds["MemoryUsage"] = new PerformanceThreshold
            {
                MetricName = "MemoryUsage",
                ThresholdValue = config.MaxMemoryUsage,
                ComparisonType = ComparisonType.GreaterThan,
                TriggerAdaptation = true,
                SendAlert = true
            };
            
            // CPU time threshold
            _performanceThresholds["CpuTime"] = new PerformanceThreshold
            {
                MetricName = "CpuTime",
                ThresholdValue = config.MaxCpuTime,
                ComparisonType = ComparisonType.GreaterThan,
                TriggerAdaptation = true,
                SendAlert = false
            };
            
            // GPU time threshold
            _performanceThresholds["GpuTime"] = new PerformanceThreshold
            {
                MetricName = "GpuTime",
                ThresholdValue = config.MaxGpuTime,
                ComparisonType = ComparisonType.GreaterThan,
                TriggerAdaptation = false,
                SendAlert = true
            };
        }
        
        private double GetMetricValue(GamePerformanceSnapshot snapshot, string metricName)
        {
            return metricName switch
            {
                "FrameRate" => snapshot.FrameRate,
                "MemoryUsage" => snapshot.MemoryUsage,
                "CpuTime" => snapshot.CpuTime,
                "GpuTime" => snapshot.GpuTime,
                "GarbageCollectionTime" => snapshot.GarbageCollectionTime,
                "DrawCalls" => snapshot.DrawCalls,
                _ => 0
            };
        }
        
        private AdaptationPriority DetermineAdaptationPriority(PerformanceAnalysis analysis)
        {
            return analysis.Severity switch
            {
                PerformanceIssueSeverity.Critical => AdaptationPriority.Critical,
                PerformanceIssueSeverity.High => AdaptationPriority.High,
                PerformanceIssueSeverity.Medium => AdaptationPriority.Medium,
                _ => AdaptationPriority.Low
            };
        }
        
        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var valueList = values.ToList();
            if (!valueList.Any()) return 0;
            
            var mean = valueList.Average();
            var variance = valueList.Sum(v => Math.Pow(v - mean, 2)) / valueList.Count;
            return Math.Sqrt(variance);
        }
        
        private IEnumerable<PerformanceTrend> AnalyzePerformanceTrends(List<GamePerformanceSnapshot> snapshots)
        {
            var trends = new List<PerformanceTrend>();
            
            if (snapshots.Count < 2) return trends;
            
            // Analyze frame rate trend
            var frameRateTrend = AnalyzeMetricTrend(snapshots, s => s.FrameRate, "FrameRate");
            if (frameRateTrend != null) trends.Add(frameRateTrend);
            
            // Analyze memory usage trend
            var memoryTrend = AnalyzeMetricTrend(snapshots, s => s.MemoryUsage, "MemoryUsage");
            if (memoryTrend != null) trends.Add(memoryTrend);
            
            // Analyze CPU time trend
            var cpuTrend = AnalyzeMetricTrend(snapshots, s => s.CpuTime, "CpuTime");
            if (cpuTrend != null) trends.Add(cpuTrend);
            
            return trends;
        }
        
        private PerformanceTrend? AnalyzeMetricTrend(List<GamePerformanceSnapshot> snapshots, Func<GamePerformanceSnapshot, double> metricSelector, string metricName)
        {
            if (snapshots.Count < 2) return null;
            
            var values = snapshots.Select(metricSelector).ToList();
            var firstHalf = values.Take(values.Count / 2).Average();
            var secondHalf = values.Skip(values.Count / 2).Average();
            
            var trendDirection = secondHalf > firstHalf ? TrendDirection.Increasing : TrendDirection.Decreasing;
            var trendStrength = Math.Abs(secondHalf - firstHalf) / firstHalf;
            
            return new PerformanceTrend
            {
                MetricName = metricName,
                Direction = trendDirection,
                Strength = trendStrength,
                StartValue = firstHalf,
                EndValue = secondHalf
            };
        }
        
        private IEnumerable<CriticalPerformanceEvent> IdentifyCriticalEvents(List<GamePerformanceSnapshot> snapshots)
        {
            var events = new List<CriticalPerformanceEvent>();
            
            foreach (var snapshot in snapshots)
            {
                if (snapshot.FrameRate < 15) // Critical frame rate drop
                {
                    events.Add(new CriticalPerformanceEvent
                    {
                        Timestamp = snapshot.Timestamp,
                        EventType = "Critical Frame Rate Drop",
                        Severity = PerformanceIssueSeverity.Critical,
                        Description = $"Frame rate dropped to {snapshot.FrameRate:F1} FPS",
                        Snapshot = snapshot
                    });
                }
                
                if (snapshot.MemoryUsage > 2L * 1024 * 1024 * 1024) // > 2GB memory usage
                {
                    events.Add(new CriticalPerformanceEvent
                    {
                        Timestamp = snapshot.Timestamp,
                        EventType = "High Memory Usage",
                        Severity = PerformanceIssueSeverity.High,
                        Description = $"Memory usage reached {snapshot.MemoryUsage / 1024 / 1024 / 1024:F1} GB",
                        Snapshot = snapshot
                    });
                }
                
                if (snapshot.GarbageCollectionTime > 10) // > 10ms GC time
                {
                    events.Add(new CriticalPerformanceEvent
                    {
                        Timestamp = snapshot.Timestamp,
                        EventType = "High Garbage Collection",
                        Severity = PerformanceIssueSeverity.High,
                        Description = $"GC time reached {snapshot.GarbageCollectionTime:F1}ms",
                        Snapshot = snapshot
                    });
                }
            }
            
            return events;
        }
        
        private async Task<IEnumerable<OptimizationOpportunity>> IdentifyOptimizationOpportunities(List<GamePerformanceSnapshot> snapshots)
        {
            var opportunities = new List<OptimizationOpportunity>();
            
            // Frame rate optimization opportunities
            var lowFrameRateSnapshots = snapshots.Where(s => s.FrameRate < 30).ToList();
            if (lowFrameRateSnapshots.Any())
            {
                opportunities.Add(new OptimizationOpportunity
                {
                    Type = "Frame Rate Optimization",
                    Description = "Frame rate consistently below 30 FPS",
                    Impact = OptimizationImpact.High,
                    EstimatedImprovement = 0.3,
                    Recommendations = new[] { "Optimize rendering pipeline", "Reduce draw calls", "Implement LOD groups" }
                });
            }
            
            // Memory optimization opportunities
            var highMemorySnapshots = snapshots.Where(s => s.MemoryUsage > 1024 * 1024 * 1024).ToList(); // > 1GB
            if (highMemorySnapshots.Any())
            {
                opportunities.Add(new OptimizationOpportunity
                {
                    Type = "Memory Optimization",
                    Description = "High memory usage detected",
                    Impact = OptimizationImpact.Medium,
                    EstimatedImprovement = 0.2,
                    Recommendations = new[] { "Optimize texture sizes", "Implement object pooling", "Reduce asset quality" }
                });
            }
            
            // GC optimization opportunities
            var highGCSnapshots = snapshots.Where(s => s.GarbageCollectionTime > 5).ToList(); // > 5ms GC
            if (highGCSnapshots.Any())
            {
                opportunities.Add(new OptimizationOpportunity
                {
                    Type = "Garbage Collection Optimization",
                    Description = "High garbage collection time detected",
                    Impact = OptimizationImpact.High,
                    EstimatedImprovement = 0.4,
                    Recommendations = new[] { "Avoid allocations in Update", "Use object pooling", "Cache frequently used objects" }
                });
            }
            
            return opportunities;
        }
        
        private IEnumerable<PlatformInsight> GeneratePlatformInsights(List<GamePerformanceSnapshot> snapshots)
        {
            var insights = new List<PlatformInsight>();
            
            // Analyze performance patterns
            var averageFrameRate = snapshots.Average(s => s.FrameRate);
            var frameRateVariance = CalculateStandardDeviation(snapshots.Select(s => s.FrameRate));
            
            insights.Add(new PlatformInsight
            {
                Platform = "Current Platform",
                InsightType = "Performance Stability",
                Description = $"Average frame rate: {averageFrameRate:F1} FPS, Variance: {frameRateVariance:F1}",
                Recommendation = frameRateVariance > 10 ? "Improve frame rate stability" : "Frame rate is stable"
            });
            
            // Memory usage insights
            var averageMemory = snapshots.Average(s => s.MemoryUsage);
            var maxMemory = snapshots.Max(s => s.MemoryUsage);
            
            insights.Add(new PlatformInsight
            {
                Platform = "Current Platform",
                InsightType = "Memory Usage",
                Description = $"Average memory: {averageMemory / 1024 / 1024:F1} MB, Peak: {maxMemory / 1024 / 1024:F1} MB",
                Recommendation = maxMemory > 1024 * 1024 * 1024 ? "Optimize memory usage" : "Memory usage is acceptable"
            });
            
            return insights;
        }
        
        private void MonitoringTimerCallback(object? state)
        {
            // Timer callback for additional monitoring tasks
            _logger.LogDebug("Monitoring timer callback executed");
        }
        
        public void Dispose()
        {
            _monitoringTimer?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
    
    /// <summary>
    /// Game performance monitor interface
    /// </summary>
    public interface IGamePerformanceMonitor : IDisposable
    {
        Task StartMonitoringAsync(GameMonitoringConfiguration config);
        Task StopMonitoringAsync();
        Task<GamePerformanceReport> GeneratePerformanceReportAsync(TimeSpan timeRange);
        Task<GamePerformanceSnapshot> GetCurrentPerformanceSnapshotAsync();
    }
    
    /// <summary>
    /// Unity profiler integration interface
    /// </summary>
    public interface IUnityProfilerIntegration
    {
        Task StartProfilingAsync(UnityProfilingConfiguration configuration);
        Task StopProfilingAsync();
        Task<UnityProfilerData> GetCurrentProfilerDataAsync();
    }
    
    /// <summary>
    /// Performance analyzer interface
    /// </summary>
    public interface IPerformanceAnalyzer
    {
        Task<PerformanceAnalysis> AnalyzeSnapshotAsync(GamePerformanceSnapshot snapshot);
    }
    
    /// <summary>
    /// Game monitoring configuration
    /// </summary>
    public class GameMonitoringConfiguration
    {
        public string GameName { get; set; } = string.Empty;
        public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(1);
        public int MaxHistorySize { get; set; } = 1000;
        public double TargetFrameRate { get; set; } = 60.0;
        public long MaxMemoryUsage { get; set; } = 1024 * 1024 * 1024; // 1GB
        public double MaxCpuTime { get; set; } = 16.67; // 60 FPS target
        public double MaxGpuTime { get; set; } = 16.67; // 60 FPS target
        public UnityProfilingConfiguration ProfilingConfiguration { get; set; } = new();
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
    }
    
    /// <summary>
    /// Game performance snapshot
    /// </summary>
    public class GamePerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public double FrameRate { get; set; }
        public double FrameTime { get; set; }
        public double CpuTime { get; set; }
        public double GpuTime { get; set; }
        public long MemoryUsage { get; set; }
        public double GarbageCollectionTime { get; set; }
        public int DrawCalls { get; set; }
        public int BatchedDrawCalls { get; set; }
        public int Triangles { get; set; }
        public int Vertices { get; set; }
        public int PlayerCount { get; set; }
        public string GameState { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Game performance report
    /// </summary>
    public class GamePerformanceReport
    {
        public TimeSpan TimeRange { get; set; }
        public int SnapshotCount { get; set; }
        public double AverageFrameRate { get; set; }
        public double MinFrameRate { get; set; }
        public double MaxFrameRate { get; set; }
        public double FrameRateStandardDeviation { get; set; }
        public IEnumerable<PerformanceTrend> PerformanceTrends { get; set; } = new List<PerformanceTrend>();
        public IEnumerable<CriticalPerformanceEvent> CriticalEvents { get; set; } = new List<CriticalPerformanceEvent>();
        public IEnumerable<OptimizationOpportunity> OptimizationOpportunities { get; set; } = new List<OptimizationOpportunity>();
        public IEnumerable<PlatformInsight> PlatformInsights { get; set; } = new List<PlatformInsight>();
        
        public static GamePerformanceReport Empty => new();
    }
    
    /// <summary>
    /// Unity profiler data
    /// </summary>
    public class UnityProfilerData
    {
        public double FrameRate { get; set; }
        public double FrameTime { get; set; }
        public double CpuTime { get; set; }
        public double GpuTime { get; set; }
        public long MemoryUsage { get; set; }
        public double GCTime { get; set; }
        public int DrawCalls { get; set; }
        public int BatchedDrawCalls { get; set; }
        public int TriangleCount { get; set; }
        public int VertexCount { get; set; }
        public int ActivePlayerCount { get; set; }
        public string CurrentGameState { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Performance analysis
    /// </summary>
    public class PerformanceAnalysis
    {
        public string PrimaryIssue { get; set; } = string.Empty;
        public PerformanceIssueSeverity Severity { get; set; }
        public bool RequiresImmediateAction { get; set; }
        public IEnumerable<string> Recommendations { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Performance threshold
    /// </summary>
    public class PerformanceThreshold
    {
        public string MetricName { get; set; } = string.Empty;
        public double ThresholdValue { get; set; }
        public ComparisonType ComparisonType { get; set; }
        public bool TriggerAdaptation { get; set; }
        public bool SendAlert { get; set; }
        
        public bool IsExceeded(double value)
        {
            return ComparisonType switch
            {
                ComparisonType.GreaterThan => value > ThresholdValue,
                ComparisonType.LessThan => value < ThresholdValue,
                ComparisonType.Equal => Math.Abs(value - ThresholdValue) < 0.001,
                _ => false
            };
        }
    }
    
    /// <summary>
    /// Performance trend
    /// </summary>
    public class PerformanceTrend
    {
        public string MetricName { get; set; } = string.Empty;
        public TrendDirection Direction { get; set; }
        public double Strength { get; set; }
        public double StartValue { get; set; }
        public double EndValue { get; set; }
    }
    
    /// <summary>
    /// Critical performance event
    /// </summary>
    public class CriticalPerformanceEvent
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = string.Empty;
        public PerformanceIssueSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public GamePerformanceSnapshot Snapshot { get; set; } = new();
    }
    
    /// <summary>
    /// Optimization opportunity
    /// </summary>
    public class OptimizationOpportunity
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public OptimizationImpact Impact { get; set; }
        public double EstimatedImprovement { get; set; }
        public IEnumerable<string> Recommendations { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Platform insight
    /// </summary>
    public class PlatformInsight
    {
        public string Platform { get; set; } = string.Empty;
        public string InsightType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }
    
    // Enums
    public enum ComparisonType
    {
        GreaterThan,
        LessThan,
        Equal
    }
    
    public enum TrendDirection
    {
        Increasing,
        Decreasing,
        Stable
    }
    
    public enum OptimizationImpact
    {
        Low,
        Medium,
        High,
        Critical
    }
}
