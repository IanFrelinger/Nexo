using System;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// System state information
/// </summary>
public record SystemState
{
    /// <summary>
    /// Current system health status
    /// </summary>
    public SystemHealth Health { get; init; }
    
    /// <summary>
    /// Current performance metrics
    /// </summary>
    public PerformanceMetrics Performance { get; init; } = new();
    
    /// <summary>
    /// Current environment profile
    /// </summary>
    public EnvironmentProfile Environment { get; init; } = new();
    
    /// <summary>
    /// Active adaptations
    /// </summary>
    public List<AdaptationRecord> ActiveAdaptations { get; init; } = new();
    
    /// <summary>
    /// Recent user feedback
    /// </summary>
    public List<UserFeedback> RecentFeedback { get; init; } = new();
    
    /// <summary>
    /// Timestamp when state was captured
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// System health status
/// </summary>
public enum SystemHealth
{
    /// <summary>
    /// System is healthy
    /// </summary>
    Healthy,
    
    /// <summary>
    /// System has minor issues
    /// </summary>
    Warning,
    
    /// <summary>
    /// System has significant issues
    /// </summary>
    Critical,
    
    /// <summary>
    /// System is offline or unavailable
    /// </summary>
    Offline
}
