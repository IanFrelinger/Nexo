namespace Nexo.Feature.Monitoring.Services;

/// <summary>
/// Interface for real-time adaptation dashboard
/// </summary>
public interface IAdaptationDashboard
{
    /// <summary>
    /// Get real-time dashboard data
    /// </summary>
    Task<AdaptationDashboardData> GetRealTimeDashboardDataAsync();
    
    /// <summary>
    /// Stream adaptation events in real-time
    /// </summary>
    IAsyncEnumerable<AdaptationEvent> StreamAdaptationEventsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get adaptation effectiveness metrics
    /// </summary>
    Task<AdaptationEffectiveness> GetAdaptationEffectivenessAsync();
    
    /// <summary>
    /// Get performance trends over time
    /// </summary>
    Task<IEnumerable<PerformanceTrend>> GetPerformanceTrendsAsync(TimeSpan timeWindow);
    
    /// <summary>
    /// Get learning insights dashboard
    /// </summary>
    Task<LearningDashboard> GetLearningDashboardAsync();
    
    /// <summary>
    /// Get environment adaptation status
    /// </summary>
    Task<EnvironmentAdaptationStatus> GetEnvironmentAdaptationStatusAsync();
}

/// <summary>
/// Real-time adaptation dashboard data
/// </summary>
public class AdaptationDashboardData
{
    public AdaptationStatus AdaptationStatus { get; set; } = new();
    public PerformanceMetrics PerformanceMetrics { get; set; } = new();
    public IEnumerable<AppliedAdaptation> RecentAdaptations { get; set; } = Enumerable.Empty<AppliedAdaptation>();
    public IEnumerable<PerformanceTrend> PerformanceTrends { get; set; } = Enumerable.Empty<PerformanceTrend>();
    public IEnumerable<LearningInsight> LearningInsights { get; set; } = Enumerable.Empty<LearningInsight>();
    public AdaptationEffectiveness AdaptationEffectiveness { get; set; } = new();
    public EnvironmentAdaptationStatus EnvironmentStatus { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Real-time adaptation event
/// </summary>
public class AdaptationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public AdaptationEventType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double? PerformanceImpact { get; set; }
    public Dictionary<string, object> EventData { get; set; } = new();
}

/// <summary>
/// Performance trend data
/// </summary>
public class PerformanceTrend
{
    public DateTime Timestamp { get; set; }
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public double ResponseTime { get; set; }
    public double Throughput { get; set; }
    public double OverallScore { get; set; }
}

/// <summary>
/// Learning dashboard data
/// </summary>
public class LearningDashboard
{
    public LearningEffectiveness LearningEffectiveness { get; set; } = new();
    public IEnumerable<LearningInsight> RecentInsights { get; set; } = Enumerable.Empty<LearningInsight>();
    public IEnumerable<LearningRecommendation> ActiveRecommendations { get; set; } = Enumerable.Empty<LearningRecommendation>();
    public Dictionary<string, double> LearningMetrics { get; set; } = new();
    public DateTime LastLearningCycle { get; set; }
}

/// <summary>
/// Environment adaptation status
/// </summary>
public class EnvironmentAdaptationStatus
{
    public DetectedEnvironment CurrentEnvironment { get; set; } = new();
    public IEnumerable<EnvironmentOptimization> ActiveOptimizations { get; set; } = Enumerable.Empty<EnvironmentOptimization>();
    public IEnumerable<EnvironmentChange> RecentChanges { get; set; } = Enumerable.Empty<EnvironmentChange>();
    public EnvironmentValidationResult ValidationResult { get; set; } = new();
    public DateTime LastEnvironmentCheck { get; set; }
}

/// <summary>
/// Adaptation effectiveness metrics
/// </summary>
public class AdaptationEffectiveness
{
    public double OverallEffectiveness { get; set; }
    public IEnumerable<AdaptationResult> AdaptationResults { get; set; } = Enumerable.Empty<AdaptationResult>();
    public Dictionary<string, double> EffectivenessByType { get; set; } = new();
    public double AverageImprovement { get; set; }
    public int TotalAdaptations { get; set; }
    public int SuccessfulAdaptations { get; set; }
    public DateTime LastCalculated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Adaptation result for effectiveness tracking
/// </summary>
public class AdaptationResult
{
    public string AdaptationId { get; set; } = string.Empty;
    public AdaptationType AdaptationType { get; set; }
    public double ExpectedImprovement { get; set; }
    public double ActualImprovement { get; set; }
    public double EffectivenessScore { get; set; }
    public DateTime AppliedAt { get; set; }
    public string StrategyId { get; set; } = string.Empty;
}

// Enums
public enum AdaptationEventType
{
    AdaptationApplied,
    PerformanceDegradation,
    ResourceConstraint,
    UserFeedback,
    EnvironmentChange,
    LearningInsight,
    ErrorOccurred
}
