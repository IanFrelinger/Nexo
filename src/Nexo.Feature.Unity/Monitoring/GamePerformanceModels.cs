using System;
using System.Collections.Generic;
using System.Threading;
using Nexo.Feature.Unity.Models;

namespace Nexo.Feature.Unity.Monitoring
{
    /// <summary>
    /// Game monitoring configuration
    /// </summary>
    public partial class GameMonitoringConfiguration
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
    public partial class GamePerformanceSnapshot
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
    public partial class GamePerformanceReport
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
    public partial class UnityProfilerData
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
    public partial class PerformanceAnalysis
    {
        public string PrimaryIssue { get; set; } = string.Empty;
        public PerformanceIssueSeverity Severity { get; set; }
        public bool RequiresImmediateAction { get; set; }
        public IEnumerable<string> Recommendations { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Performance threshold
    /// </summary>
    public partial class PerformanceThreshold
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
    public partial class PerformanceTrend
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
    public partial class CriticalPerformanceEvent
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
    public partial class OptimizationOpportunity
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
    public partial class PlatformInsight
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
