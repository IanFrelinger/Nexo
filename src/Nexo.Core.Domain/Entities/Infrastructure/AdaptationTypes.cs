using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Adaptation types and related entities
/// </summary>
public enum AdaptationType
{
    Performance,
    UserExperience,
    Resource,
    Feature,
    Security,
    Reliability,
    PerformanceOptimization,
    ResourceOptimization,
    UserExperienceOptimization,
    EnvironmentOptimization
}

/// <summary>
/// Optimization level
/// </summary>
public enum OptimizationLevel
{
    None,
    Basic,
    Moderate,
    Balanced, // Alias for Moderate
    Aggressive,
    Maximum
}

/// <summary>
/// Adaptation priority
/// </summary>
public enum AdaptationPriority
{
    Low = 1,
    Normal = 2,
    Medium = 2, // Alias for Normal
    High = 3,
    Critical = 4
}

/// <summary>
/// Adaptation trigger
/// </summary>
public enum AdaptationTrigger
{
    PerformanceDegradation,
    UserFeedback,
    ResourceConstraint,
    EnvironmentChange,
    Schedule,
    Manual,
    HighSeverityFeedback
}

/// <summary>
/// Adaptation record
/// </summary>
public record AdaptationRecord
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Adaptation type
    /// </summary>
    public AdaptationType Type { get; init; }
    
    /// <summary>
    /// Success flag
    /// </summary>
    public bool Success { get; init; } = false;
    
    /// <summary>
    /// Timestamp when adaptation was applied
    /// </summary>
    public DateTime AppliedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp (alias for AppliedAt)
    /// </summary>
    public DateTime Timestamp => AppliedAt;
    
    /// <summary>
    /// Strategy ID
    /// </summary>
    public string StrategyId { get; init; } = string.Empty;
    
    /// <summary>
    /// Effectiveness score (0.0 to 1.0)
    /// </summary>
    public double EffectivenessScore { get; init; } = 0.0;
    
    /// <summary>
    /// Trigger that caused this adaptation
    /// </summary>
    public AdaptationTrigger Trigger { get; init; } = AdaptationTrigger.Manual;
    
    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Additional data
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();
    
    /// <summary>
    /// Whether adaptation was successful (alias for Success)
    /// </summary>
    public bool WasSuccessful => Success;
}

/// <summary>
/// Adaptation need
/// </summary>
public record AdaptationNeed
{
    /// <summary>
    /// Adaptation type
    /// </summary>
    public AdaptationType Type { get; init; }
    
    /// <summary>
    /// Priority level
    /// </summary>
    public AdaptationPriority Priority { get; init; } = AdaptationPriority.Normal;
    
    /// <summary>
    /// Trigger that identified this need
    /// </summary>
    public AdaptationTrigger Trigger { get; init; } = AdaptationTrigger.Manual;
    
    /// <summary>
    /// Description of the need
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Supporting data
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();
}

/// <summary>
/// Adaptation context
/// </summary>
public record AdaptationContext
{
    /// <summary>
    /// Trigger that initiated adaptation
    /// </summary>
    public AdaptationTrigger Trigger { get; init; } = AdaptationTrigger.Manual;
    
    /// <summary>
    /// Priority level
    /// </summary>
    public AdaptationPriority Priority { get; init; } = AdaptationPriority.Normal;
    
    /// <summary>
    /// Context information
    /// </summary>
    public string Context { get; init; } = string.Empty;
    
    /// <summary>
    /// User feedback (if applicable)
    /// </summary>
    public UserFeedback? UserFeedback { get; init; }
    
    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Additional properties
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// Adaptation recommendation
/// </summary>
public record AdaptationRecommendation
{
    /// <summary>
    /// Recommended adaptation type
    /// </summary>
    public AdaptationType Type { get; init; }
    
    /// <summary>
    /// Priority level
    /// </summary>
    public AdaptationPriority Priority { get; init; } = AdaptationPriority.Normal;
    
    /// <summary>
    /// Confidence in recommendation (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; init; } = 0.0;
    
    /// <summary>
    /// Reasoning for recommendation
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;
    
    /// <summary>
    /// Estimated impact
    /// </summary>
    public string EstimatedImpact { get; init; } = string.Empty;
    
    /// <summary>
    /// Supporting data
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();
}

/// <summary>
/// Workload priority levels
/// </summary>
public enum WorkloadPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Network types
/// </summary>
public enum NetworkType
{
    Local,
    LAN,
    WAN,
    Internet
}

/// <summary>
/// Adaptation engine status
/// </summary>
public enum AdaptationEngineStatus
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Error
}

/// <summary>
/// Applied adaptation record
/// </summary>
public record AppliedAdaptation
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Adaptation type
    /// </summary>
    public string Type { get; init; } = string.Empty;
    
    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// When adaptation was applied
    /// </summary>
    public DateTime AppliedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Estimated improvement factor
    /// </summary>
    public double EstimatedImprovementFactor { get; init; } = 0.0;
    
    /// <summary>
    /// Actual improvement achieved
    /// </summary>
    public double ActualImprovement { get; init; } = 0.0;
    
    /// <summary>
    /// Strategy ID used
    /// </summary>
    public string StrategyId { get; init; } = string.Empty;
    
    /// <summary>
    /// Parameters used
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Adaptation improvement record
/// </summary>
public record AdaptationImprovement
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Type of improvement
    /// </summary>
    public string Type { get; init; } = string.Empty;
    
    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// When improvement was applied
    /// </summary>
    public DateTime AppliedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Improvement factor
    /// </summary>
    public double ImprovementFactor { get; init; } = 0.0;
    
    /// <summary>
    /// Adaptation ID
    /// </summary>
    public string AdaptationId { get; init; } = string.Empty;
    
    /// <summary>
    /// Improvement percentage
    /// </summary>
    public double ImprovementPercentage { get; init; } = 0.0;
    
    /// <summary>
    /// Metric name
    /// </summary>
    public string Metric { get; init; } = string.Empty;
    
    /// <summary>
    /// When measured
    /// </summary>
    public DateTime MeasuredAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Resource constraint types
/// </summary>
public enum ResourceConstraintType
{
    None,
    Cpu,
    Memory,
    Disk,
    Network
}

/// <summary>
/// Performance severity levels
/// </summary>
public enum PerformanceSeverity
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}