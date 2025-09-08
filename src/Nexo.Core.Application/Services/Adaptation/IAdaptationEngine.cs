using Microsoft.Extensions.Hosting;

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
    Task<IEnumerable<AdaptationRecord>> GetRecentAdaptationsAsync(TimeSpan timeWindow);
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

/// <summary>
/// Performance metrics for adaptation decisions
/// </summary>
public class PerformanceMetrics
{
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public double NetworkLatency { get; set; }
    public double ResponseTime { get; set; }
    public double Throughput { get; set; }
    public PerformanceSeverity Severity { get; set; }
    public bool RequiresOptimization { get; set; }
    public bool HasIterationBottlenecks { get; set; }
    public double OverallScore { get; set; }
}

/// <summary>
/// Environment profile for adaptation
/// </summary>
public class EnvironmentProfile
{
    public EnvironmentContext Context { get; set; }
    public PlatformType PlatformType { get; set; }
    public int CpuCores { get; set; }
    public long AvailableMemoryMB { get; set; }
    public NetworkProfile NetworkProfile { get; set; } = new();
    public bool HasChanged { get; set; }
}

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

/// <summary>
/// User feedback for adaptation
/// </summary>
public class UserFeedback
{
    public string FeedbackId { get; set; } = string.Empty;
    public FeedbackType Type { get; set; }
    public FeedbackSeverity Severity { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

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

/// <summary>
/// Applied adaptation record
/// </summary>
public class AppliedAdaptation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public double EstimatedImprovementFactor { get; set; }
    public double ActualImprovement { get; set; }
    public string StrategyId { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Adaptation improvement record
/// </summary>
public class AdaptationImprovement
{
    public string AdaptationId { get; set; } = string.Empty;
    public double ImprovementPercentage { get; set; }
    public string Metric { get; set; } = string.Empty;
    public DateTime MeasuredAt { get; set; }
}

/// <summary>
/// Adaptation record for history
/// </summary>
public class AdaptationRecord
{
    public string Id { get; set; } = string.Empty;
    public AdaptationType Type { get; set; }
    public AdaptationTrigger Trigger { get; set; }
    public DateTime AppliedAt { get; set; }
    public string StrategyId { get; set; } = string.Empty;
    public bool WasSuccessful { get; set; }
    public double EffectivenessScore { get; set; }
}

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

// Enums
public enum AdaptationTrigger
{
    PerformanceDegradation,
    ResourceConstraint,
    UserFeedback,
    EnvironmentChange,
    ScheduledMaintenance,
    HighSeverityFeedback,
    ManualTrigger
}

public enum AdaptationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum AdaptationType
{
    PerformanceOptimization,
    ResourceOptimization,
    UserExperienceOptimization,
    EnvironmentOptimization,
    SecurityOptimization,
    ReliabilityOptimization
}

public enum PerformanceSeverity
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum EnvironmentContext
{
    Development,
    Testing,
    Staging,
    Production
}

public enum ResourceConstraintType
{
    None,
    Cpu,
    Memory,
    Disk,
    Network
}

public enum WorkloadPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public enum NetworkType
{
    Local,
    LAN,
    WAN,
    Internet
}

public enum FeedbackType
{
    Performance,
    Usability,
    Feature,
    Bug,
    Suggestion
}

public enum FeedbackSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum AdaptationEngineStatus
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Error
}