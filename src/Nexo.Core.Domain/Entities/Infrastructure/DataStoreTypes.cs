using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Performance data summary
/// </summary>
public record PerformanceDataSummary
{
    /// <summary>
    /// Total number of data points
    /// </summary>
    public int TotalDataPoints { get; init; }
    
    /// <summary>
    /// Time range covered
    /// </summary>
    public TimeSpan TimeRange { get; init; }
    
    /// <summary>
    /// Average values by metric
    /// </summary>
    public Dictionary<string, double> AverageValues { get; init; } = new();
    
    /// <summary>
    /// Maximum values by metric
    /// </summary>
    public Dictionary<string, double> MaximumValues { get; init; } = new();
    
    /// <summary>
    /// Minimum values by metric
    /// </summary>
    public Dictionary<string, double> MinimumValues { get; init; } = new();
    
    /// <summary>
    /// Unique metrics recorded
    /// </summary>
    public List<string> Metrics { get; init; } = new();
}

/// <summary>
/// Correlation information
/// </summary>
public record Correlation
{
    /// <summary>
    /// Correlation strength
    /// </summary>
    public double Strength { get; init; } = 0.0;
    
    /// <summary>
    /// Direction of correlation
    /// </summary>
    public string Direction { get; init; } = "Unknown";
    
    /// <summary>
    /// Description of the correlation
    /// </summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Environment context
/// </summary>
public record EnvironmentContext
{
    /// <summary>
    /// Context type
    /// </summary>
    public string Type { get; init; } = "Unknown";
    
    /// <summary>
    /// Properties
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
    
    /// <summary>
    /// Development context
    /// </summary>
    public static EnvironmentContext Development => new() { Type = "Development" };
    
    /// <summary>
    /// Production context
    /// </summary>
    public static EnvironmentContext Production => new() { Type = "Production" };
}

/// <summary>
/// Resource allocation
/// </summary>
public record ResourceAllocation
{
    /// <summary>
    /// CPU allocation percentage
    /// </summary>
    public double CpuAllocation { get; init; } = 0.0;
    
    /// <summary>
    /// Memory allocation in MB
    /// </summary>
    public double MemoryAllocation { get; init; } = 0.0;
    
    /// <summary>
    /// Storage allocation in GB
    /// </summary>
    public double StorageAllocation { get; init; } = 0.0;
}

/// <summary>
/// Resource constraints
/// </summary>
public record ResourceConstraints
{
    /// <summary>
    /// Maximum CPU usage
    /// </summary>
    public double MaxCpuUsage { get; init; } = 100.0;
    
    /// <summary>
    /// Maximum memory usage in MB
    /// </summary>
    public double MaxMemoryUsage { get; init; } = 1024.0;
    
    /// <summary>
    /// Maximum storage usage in GB
    /// </summary>
    public double MaxStorageUsage { get; init; } = 100.0;
}

/// <summary>
/// Feedback severity
/// </summary>
public enum FeedbackSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Feedback store interface
/// </summary>
public interface IFeedbackStore
{
    /// <summary>
    /// Store feedback
    /// </summary>
    Task StoreFeedbackAsync(UserFeedback feedback);
    
    /// <summary>
    /// Get feedback by ID
    /// </summary>
    Task<UserFeedback?> GetFeedbackAsync(string id);
    
    /// <summary>
    /// Get feedback within time range
    /// </summary>
    Task<IEnumerable<UserFeedback>> GetFeedbackAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Delete old feedback
    /// </summary>
    Task DeleteOldFeedbackAsync(DateTime cutoffTime);
}

/// <summary>
/// Environment data store interface
/// </summary>
public interface IEnvironmentDataStore
{
    /// <summary>
    /// Store environment data
    /// </summary>
    Task StoreEnvironmentDataAsync(EnvironmentProfile environment);
    
    /// <summary>
    /// Get environment data within time range
    /// </summary>
    Task<IEnumerable<EnvironmentProfile>> GetEnvironmentDataAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get latest environment data
    /// </summary>
    Task<EnvironmentProfile?> GetLatestEnvironmentDataAsync();
    
    /// <summary>
    /// Store environment change
    /// </summary>
    Task StoreEnvironmentChangeAsync(EnvironmentChange change);
    
    /// <summary>
    /// Get environment changes within time range
    /// </summary>
    Task<IEnumerable<EnvironmentChange>> GetEnvironmentChangesAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Delete old environment data
    /// </summary>
    Task DeleteOldEnvironmentDataAsync(DateTime cutoffTime);
}

/// <summary>
/// Resource utilization data
/// </summary>
public record ResourceUtilization
{
    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsage { get; init; } = 0.0;
    
    /// <summary>
    /// Memory usage in MB
    /// </summary>
    public double MemoryUsage { get; init; } = 0.0;
    
    /// <summary>
    /// Disk usage in MB
    /// </summary>
    public double DiskUsage { get; init; } = 0.0;
    
    /// <summary>
    /// Network usage in MB
    /// </summary>
    public double NetworkUsage { get; init; } = 0.0;
    
    /// <summary>
    /// When measured
    /// </summary>
    public DateTime MeasuredAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Active workload information
/// </summary>
public record ActiveWorkload
{
    /// <summary>
    /// Workload ID
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Workload name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Priority level
    /// </summary>
    public WorkloadPriority Priority { get; init; } = WorkloadPriority.Normal;
    
    /// <summary>
    /// Resource requirements
    /// </summary>
    public ResourceUtilization Requirements { get; init; } = new();
    
    /// <summary>
    /// When started
    /// </summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Estimated duration
    /// </summary>
    public TimeSpan EstimatedDuration { get; init; } = TimeSpan.Zero;
    
    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// Adaptation status information
/// </summary>
public record AdaptationStatus
{
    /// <summary>
    /// Current status
    /// </summary>
    public AdaptationEngineStatus Status { get; init; } = AdaptationEngineStatus.Stopped;
    
    /// <summary>
    /// Last adaptation time
    /// </summary>
    public DateTime LastAdaptation { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Total adaptations applied
    /// </summary>
    public int TotalAdaptations { get; init; } = 0;
    
    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; init; } = 0.0;
    
    /// <summary>
    /// Current performance level
    /// </summary>
    public PerformanceLevel PerformanceLevel { get; init; } = PerformanceLevel.Medium;
    
    /// <summary>
    /// Is learning enabled
    /// </summary>
    public bool IsLearningEnabled { get; init; } = true;
    
    /// <summary>
    /// Last error message
    /// </summary>
    public string LastError { get; init; } = string.Empty;
}

/// <summary>
/// Performance level enumeration
/// </summary>
public enum PerformanceLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Metrics aggregation
/// </summary>
public record MetricsAggregation
{
    /// <summary>
    /// Aggregated metrics
    /// </summary>
    public Dictionary<string, double> Metrics { get; init; } = new();
    
    /// <summary>
    /// Time range
    /// </summary>
    public TimeSpan TimeRange { get; init; } = TimeSpan.Zero;
    
    /// <summary>
    /// When aggregated
    /// </summary>
    public DateTime AggregatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Metrics summary
/// </summary>
public record MetricsSummary
{
    /// <summary>
    /// Total metrics
    /// </summary>
    public int TotalMetrics { get; init; } = 0;
    
    /// <summary>
    /// Average values
    /// </summary>
    public Dictionary<string, double> AverageValues { get; init; } = new();
    
    /// <summary>
    /// Maximum values
    /// </summary>
    public Dictionary<string, double> MaximumValues { get; init; } = new();
    
    /// <summary>
    /// Minimum values
    /// </summary>
    public Dictionary<string, double> MinimumValues { get; init; } = new();
    
    /// <summary>
    /// Time range
    /// </summary>
    public TimeSpan TimeRange { get; init; } = TimeSpan.Zero;
}

/// <summary>
/// Metrics trend
/// </summary>
public record MetricsTrend
{
    /// <summary>
    /// Metric name
    /// </summary>
    public string MetricName { get; init; } = string.Empty;
    
    /// <summary>
    /// Trend direction
    /// </summary>
    public string Direction { get; init; } = string.Empty;
    
    /// <summary>
    /// Trend strength
    /// </summary>
    public double Strength { get; init; } = 0.0;
    
    /// <summary>
    /// Time period
    /// </summary>
    public TimeSpan TimePeriod { get; init; } = TimeSpan.Zero;
}

/// <summary>
/// Metrics statistics
/// </summary>
public record MetricsStatistics
{
    /// <summary>
    /// Total metrics count
    /// </summary>
    public int TotalCount { get; init; } = 0;
    
    /// <summary>
    /// Unique metrics count
    /// </summary>
    public int UniqueMetricsCount { get; init; } = 0;
    
    /// <summary>
    /// Time range covered
    /// </summary>
    public TimeSpan TimeRange { get; init; } = TimeSpan.Zero;
    
    /// <summary>
    /// Last updated
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Code generation metrics
/// </summary>
public record CodeGenerationMetrics
{
    /// <summary>
    /// Total code generated
    /// </summary>
    public int TotalCodeGenerated { get; init; } = 0;
    
    /// <summary>
    /// Average code length
    /// </summary>
    public double AverageCodeLength { get; init; } = 0.0;
    
    /// <summary>
    /// Average generation time
    /// </summary>
    public TimeSpan AverageGenerationTime { get; init; } = TimeSpan.Zero;
    
    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; init; } = 0.0;
    
    /// <summary>
    /// When measured
    /// </summary>
    public DateTime MeasuredAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Optimization result
/// </summary>
public record OptimizationResult
{
    /// <summary>
    /// Whether optimization was successful
    /// </summary>
    public bool IsSuccessful { get; init; } = false;
    
    /// <summary>
    /// Optimized code
    /// </summary>
    public string OptimizedCode { get; init; } = string.Empty;
    
    /// <summary>
    /// Performance improvement
    /// </summary>
    public double PerformanceImprovement { get; init; } = 0.0;
    
    /// <summary>
    /// Memory improvement
    /// </summary>
    public double MemoryImprovement { get; init; } = 0.0;
    
    /// <summary>
    /// Optimization suggestions
    /// </summary>
    public IEnumerable<string> Suggestions { get; init; } = new List<string>();
    
    /// <summary>
    /// When optimized
    /// </summary>
    public DateTime OptimizedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Code validation result
/// </summary>
public record CodeValidationResult
{
    /// <summary>
    /// Whether code is valid
    /// </summary>
    public bool IsValid { get; init; } = false;
    
    /// <summary>
    /// Validation errors
    /// </summary>
    public IEnumerable<string> Errors { get; init; } = new List<string>();
    
    /// <summary>
    /// Validation warnings
    /// </summary>
    public IEnumerable<string> Warnings { get; init; } = new List<string>();
    
    /// <summary>
    /// Validation score
    /// </summary>
    public double ValidationScore { get; init; } = 0.0;
    
    /// <summary>
    /// When validated
    /// </summary>
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// User experience analysis
/// </summary>
public record UserExperienceAnalysis
{
    /// <summary>
    /// Overall user experience score
    /// </summary>
    public double OverallScore { get; init; } = 0.0;
    
    /// <summary>
    /// Performance score
    /// </summary>
    public double PerformanceScore { get; init; } = 0.0;
    
    /// <summary>
    /// Usability score
    /// </summary>
    public double UsabilityScore { get; init; } = 0.0;
    
    /// <summary>
    /// Satisfaction score
    /// </summary>
    public double SatisfactionScore { get; init; } = 0.0;
    
    /// <summary>
    /// When analyzed
    /// </summary>
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// User experience trend
/// </summary>
public record UserExperienceTrend
{
    /// <summary>
    /// Trend name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Trend direction
    /// </summary>
    public string Direction { get; init; } = string.Empty;
    
    /// <summary>
    /// Trend strength
    /// </summary>
    public double Strength { get; init; } = 0.0;
    
    /// <summary>
    /// Time period
    /// </summary>
    public TimeSpan TimePeriod { get; init; } = TimeSpan.Zero;
}

/// <summary>
/// User experience metrics
/// </summary>
public record UserExperienceMetrics
{
    /// <summary>
    /// Response time
    /// </summary>
    public double ResponseTime { get; init; } = 0.0;
    
    /// <summary>
    /// Error rate
    /// </summary>
    public double ErrorRate { get; init; } = 0.0;
    
    /// <summary>
    /// User satisfaction
    /// </summary>
    public double UserSatisfaction { get; init; } = 0.0;
    
    /// <summary>
    /// Task completion rate
    /// </summary>
    public double TaskCompletionRate { get; init; } = 0.0;
    
    /// <summary>
    /// When measured
    /// </summary>
    public DateTime MeasuredAt { get; init; } = DateTime.UtcNow;
}
