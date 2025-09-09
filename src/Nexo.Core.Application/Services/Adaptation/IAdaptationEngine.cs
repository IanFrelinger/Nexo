
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Core adaptation engine that orchestrates real-time system improvements
/// </summary>
public interface IAdaptationEngine
{
    /// <summary>
    /// Start the adaptation engine with continuous monitoring
    /// </summary>
    Task StartAdaptationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop the adaptation engine
    /// </summary>
    Task StopAdaptationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Trigger immediate adaptation based on current metrics
    /// </summary>
    Task TriggerAdaptationAsync(AdaptationContext context);
    
    /// <summary>
    /// Register an adaptation strategy for specific scenarios
    /// </summary>
    void RegisterAdaptationStrategy(IAdaptationStrategy strategy);
    
    /// <summary>
    /// Get current adaptation status and applied optimizations
    /// </summary>
    Task<AdaptationStatus> GetAdaptationStatusAsync();
    
    /// <summary>
    /// Get recent adaptation history
    /// </summary>
    Task<IEnumerable<Nexo.Core.Domain.Entities.Infrastructure.AdaptationRecord>> GetRecentAdaptationsAsync(TimeSpan timeWindow);
}

/// <summary>
/// Context for triggering adaptations
/// </summary>
public class AdaptationContext
{
    public AdaptationTrigger Trigger { get; set; }
    public AdaptationPriority Priority { get; set; }
    public SystemState? Context { get; set; }
    public UserFeedback? UserFeedback { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Current system state for adaptation decisions
/// </summary>
public class SystemState
{
    public PerformanceMetrics PerformanceMetrics { get; set; } = new();
    public EnvironmentProfile EnvironmentProfile { get; set; } = new();
    public IEnumerable<UserFeedback> RecentFeedback { get; set; } = Enumerable.Empty<UserFeedback>();
    public ResourceUtilization ResourceUtilization { get; set; } = new();
    public IEnumerable<ActiveWorkload> ActiveWorkloads { get; set; } = Enumerable.Empty<ActiveWorkload>();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// Using Domain layer types: PerformanceMetrics, EnvironmentProfile

/// <summary>
/// Resource utilization information
/// </summary>
public class ResourceUtilization
{
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public double NetworkUsage { get; set; }
    public bool IsConstrained { get; set; }
    public ResourceConstraintType ConstraintType { get; set; }
}

/// <summary>
/// Active workload information
/// </summary>
public class ActiveWorkload
{
    public string WorkloadId { get; set; } = string.Empty;
    public string WorkloadType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public double ResourceUsage { get; set; }
    public WorkloadPriority Priority { get; set; }
}

/// <summary>
/// Network profile information
/// </summary>
public class NetworkProfile
{
    public double Bandwidth { get; set; }
    public double Latency { get; set; }
    public bool IsReliable { get; set; }
    public NetworkType Type { get; set; }
}

// Using Domain layer type: UserFeedback

/// <summary>
/// Adaptation status information
/// </summary>
public class AdaptationStatus
{
    public AdaptationEngineStatus EngineStatus { get; set; }
    public IEnumerable<AppliedAdaptation> ActiveAdaptations { get; set; } = Enumerable.Empty<AppliedAdaptation>();
    public IEnumerable<AdaptationImprovement> RecentImprovements { get; set; } = Enumerable.Empty<AdaptationImprovement>();
    public DateTime LastAdaptationTime { get; set; }
    public int TotalAdaptationsApplied { get; set; }
    public double OverallEffectiveness { get; set; }
}

// Using Domain layer types: AppliedAdaptation, AdaptationImprovement, AdaptationRecord

/// <summary>
/// Adaptation need identified by the engine
/// </summary>
public class AdaptationNeed
{
    public AdaptationType Type { get; set; }
    public AdaptationTrigger Trigger { get; set; }
    public AdaptationPriority Priority { get; set; }
    public SystemState Context { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Result of an adaptation execution
/// </summary>
public class AdaptationResult
{
    public bool IsSuccessful { get; set; }
    public IEnumerable<AppliedAdaptation> AppliedAdaptations { get; set; } = Enumerable.Empty<AppliedAdaptation>();
    public double EstimatedImprovement { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

// Using Domain layer enums: AdaptationTrigger, AdaptationPriority, AdaptationType

// Using Domain layer enums: PerformanceSeverity, EnvironmentContext, ResourceConstraintType, WorkloadPriority, NetworkType, FeedbackType, FeedbackSeverity, AdaptationEngineStatus