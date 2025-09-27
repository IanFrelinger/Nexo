using System;
using System.Collections.Generic;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// Performance Optimization Models
    /// </summary>
    public partial class PerformanceOptimizationInitializationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public List<string> AvailableOptimizations { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime InitializationTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance tuning profile.
    /// </summary>
    public partial class PerformanceTuningProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TuningProfileType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public List<PlatformType> SupportedPlatforms { get; set; } = new List<PlatformType>();
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Result of performance tuning application.
    /// </summary>
    public partial class PerformanceTuningResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public List<string> AppliedOptimizations { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ApplicationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Memory optimization strategy.
    /// </summary>
    public partial class MemoryOptimizationStrategy
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MemoryOptimizationType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public bool IsAggressive { get; set; }
        public List<string> TargetAreas { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of memory optimization.
    /// </summary>
    public partial class MemoryOptimizationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public long MemoryFreed { get; set; }
        public long MemoryBefore { get; set; }
        public long MemoryAfter { get; set; }
        public List<string> OptimizationsApplied { get; set; } = new List<string>();
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Battery optimization strategy.
    /// </summary>
    public partial class BatteryOptimizationStrategy
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BatteryOptimizationType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public bool IsPowerSaving { get; set; }
        public List<string> PowerSavingFeatures { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of battery optimization.
    /// </summary>
    public partial class BatteryOptimizationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public double EstimatedBatterySavings { get; set; }
        public List<string> OptimizationsApplied { get; set; } = new List<string>();
        public List<string> PowerSavingFeatures { get; set; } = new List<string>();
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance monitoring configuration.
    /// </summary>
    public partial class PerformanceMonitoringConfig
    {
        public string Name { get; set; } = string.Empty;
        public bool EnableCPUMonitoring { get; set; } = true;
        public bool EnableMemoryMonitoring { get; set; } = true;
        public bool EnableBatteryMonitoring { get; set; } = true;
        public bool EnableNetworkMonitoring { get; set; } = false;
        public int MonitoringInterval { get; set; } = 1000; // milliseconds
        public List<string> CustomMetrics { get; set; } = new List<string>();
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of performance monitoring operations.
    /// </summary>
    public partial class PerformanceMonitoringResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsMonitoring { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? StopTime { get; set; }
        public List<string> MonitoredMetrics { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance metrics data.
    /// </summary>
    public partial class PerformanceMetricsResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CollectionTime { get; set; } = DateTime.UtcNow;
        public double CPUsage { get; set; }
        public long MemoryUsage { get; set; }
        public long AvailableMemory { get; set; }
        public double BatteryLevel { get; set; }
        public bool IsCharging { get; set; }
        public double NetworkLatency { get; set; }
        public Dictionary<string, object> CustomMetrics { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance analysis result.
    /// </summary>
    public partial class PerformanceAnalysisResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;
        public List<PerformanceBottleneck> Bottlenecks { get; set; } = new List<PerformanceBottleneck>();
        public List<PerformanceRecommendation> Recommendations { get; set; } = new List<PerformanceRecommendation>();
        public double OverallPerformanceScore { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents a performance bottleneck.
    /// </summary>
    public partial class PerformanceBottleneck
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public BottleneckType Type { get; set; }
        public string Severity { get; set; } = string.Empty;
        public double Impact { get; set; }
        public List<string> Solutions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a performance recommendation.
    /// </summary>
    public partial class PerformanceRecommendation
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RecommendationType Type { get; set; }
        public double ExpectedImprovement { get; set; }
        public string Priority { get; set; } = string.Empty;
        public List<string> ImplementationSteps { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of performance recommendations.
    /// </summary>
    public partial class PerformanceRecommendationsResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public PlatformType PlatformType { get; set; }
        public List<PerformanceRecommendation> Recommendations { get; set; } = new List<PerformanceRecommendation>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of automatic optimization.
    /// </summary>
    public partial class AutomaticOptimizationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> AppliedOptimizations { get; set; } = new List<string>();
        public List<string> SkippedOptimizations { get; set; } = new List<string>();
        public double PerformanceImprovement { get; set; }
        public TimeSpan OptimizationTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Performance optimization settings.
    /// </summary>
    public partial class PerformanceOptimizationSettings
    {
        public bool EnableAutomaticOptimization { get; set; } = true;
        public bool EnableMemoryOptimization { get; set; } = true;
        public bool EnableBatteryOptimization { get; set; } = true;
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public int OptimizationInterval { get; set; } = 300000; // 5 minutes
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of performance validation.
    /// </summary>
    public partial class PerformanceValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<string> ValidationWarnings { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of performance reset.
    /// </summary>
    public partial class PerformanceResetResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ResetSettings { get; set; } = new List<string>();
        public DateTime ResetTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of performance disposal.
    /// </summary>
    public partial class PerformanceDisposalResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DisposedResources { get; set; }
        public List<string> DisposedComponents { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
