using System;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Learning insight from system analysis
/// </summary>
public record LearningInsight
{
    /// <summary>
    /// Unique identifier for the insight
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Type of insight
    /// </summary>
    public InsightType Type { get; init; }
    
    /// <summary>
    /// Insight description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence level (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Timestamp when insight was generated
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Discovered at timestamp (alias for Timestamp)
    /// </summary>
    public DateTime DiscoveredAt => Timestamp;
    
    /// <summary>
    /// Supporting data
    /// </summary>
    public Dictionary<string, object> SupportingData { get; init; } = new();
    
    /// <summary>
    /// Data (alias for SupportingData)
    /// </summary>
    public Dictionary<string, object> Data => SupportingData;
    
    /// <summary>
    /// Whether insight has been applied
    /// </summary>
    public bool IsApplied { get; init; } = false;
}

/// <summary>
/// Types of learning insights
/// </summary>
public enum InsightType
{
    /// <summary>
    /// Performance pattern
    /// </summary>
    PerformancePattern,
    
    /// <summary>
    /// User behavior pattern
    /// </summary>
    UserBehavior,
    
    /// <summary>
    /// System optimization opportunity
    /// </summary>
    OptimizationOpportunity,
    
    /// <summary>
    /// Error pattern
    /// </summary>
    ErrorPattern,
    
    /// <summary>
    /// Resource usage pattern
    /// </summary>
    ResourceUsage
}
