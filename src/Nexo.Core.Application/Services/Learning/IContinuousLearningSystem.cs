using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Learning;

/// <summary>
/// Interface for continuous learning system that improves over time
/// </summary>
public interface IContinuousLearningSystem
{
    /// <summary>
    /// Process a learning cycle to identify patterns and improve system behavior
    /// </summary>
    Task ProcessLearningCycleAsync();
    
    /// <summary>
    /// Get learning recommendations for a specific context
    /// </summary>
    Task<LearningRecommendations> GetRecommendationsForContext(LearningContext context);
    
    /// <summary>
    /// Record the results of an adaptation for learning
    /// </summary>
    Task RecordAdaptationResultsAsync(IEnumerable<AdaptationNeed> adaptations);
    
    /// <summary>
    /// Get current learning insights
    /// </summary>
    Task<IEnumerable<Nexo.Core.Domain.Entities.Infrastructure.LearningInsight>> GetCurrentInsightsAsync();
    
    /// <summary>
    /// Get learning effectiveness metrics
    /// </summary>
    Task<LearningEffectiveness> GetLearningEffectivenessAsync();
}

/// <summary>
/// Interface for pattern recognition in learning data
/// </summary>
public interface IPatternRecognitionEngine
{
    /// <summary>
    /// Identify patterns in feedback and performance data
    /// </summary>
    Task<IEnumerable<IdentifiedPattern>> IdentifyPatternsAsync(
        IEnumerable<UserFeedback> feedback, 
        IEnumerable<PerformanceData> performanceData);
    
    /// <summary>
    /// Find similar contexts in historical data
    /// </summary>
    Task<IEnumerable<HistoricalContext>> FindSimilarContextsAsync(LearningContext context);
    
    /// <summary>
    /// Analyze correlation between different factors
    /// </summary>
    Task<CorrelationAnalysis> AnalyzeCorrelationsAsync(
        IEnumerable<PerformanceData> performanceData,
        IEnumerable<AdaptationRecord> adaptations);
}

/// <summary>
/// Interface for adaptation recommendation engine
/// </summary>
public interface IAdaptationRecommender
{
    /// <summary>
    /// Generate recommendations based on learning insights
    /// </summary>
    Task<IEnumerable<AdaptationRecommendation>> GenerateRecommendationsAsync(
        IEnumerable<LearningInsight> insights);
    
    /// <summary>
    /// Get recommendations for immediate application
    /// </summary>
    Task<IEnumerable<AdaptationRecommendation>> GetImmediateRecommendationsAsync();
    
    /// <summary>
    /// Get recommendations for future improvements
    /// </summary>
    Task<IEnumerable<AdaptationRecommendation>> GetFutureRecommendationsAsync();
}

/// <summary>
/// Learning context for recommendations
/// </summary>
public class LearningContext
{
    public SystemState CurrentState { get; set; } = new();
    public IEnumerable<UserFeedback> RecentFeedback { get; set; } = Enumerable.Empty<UserFeedback>();
    public IEnumerable<PerformanceData> RecentPerformance { get; set; } = Enumerable.Empty<PerformanceData>();
    public EnvironmentProfile Environment { get; set; } = new();
    public string ContextType { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Learning recommendations for a context
/// </summary>
public class LearningRecommendations
{
    public IEnumerable<LearningRecommendation> Recommendations { get; set; } = Enumerable.Empty<LearningRecommendation>();
    public double OverallConfidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    public void Add(LearningRecommendation recommendation)
    {
        var list = Recommendations.ToList();
        list.Add(recommendation);
        Recommendations = list;
    }
}

/// <summary>
/// Individual learning recommendation
/// </summary>
public class LearningRecommendation
{
    public string Approach { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double ExpectedSuccessRate { get; set; }
    public int SupportingEvidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public AdaptationType RecommendedAdaptationType { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Learning insight derived from pattern analysis
/// </summary>
public class LearningInsight
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public InsightType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
    public Dictionary<string, object> SupportingData { get; set; } = new();
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
    public bool IsApplied { get; set; }
}

/// <summary>
/// Identified pattern from data analysis
/// </summary>
public class IdentifiedPattern
{
    public string PatternId { get; set; } = Guid.NewGuid().ToString();
    public PatternType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public double StatisticalSignificance { get; set; }
    public double CorrelationStrength { get; set; }
    public string OptimalStrategy { get; set; } = string.Empty;
    public string PreferredApproach { get; set; } = string.Empty;
    public string AlternativeApproach { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public double ImprovementFactor { get; set; }
    public double UserPreferenceStrength { get; set; }
    public double PlatformSpecificConfidence { get; set; }
    public Dictionary<string, object> SupportingData { get; set; } = new();
}

/// <summary>
/// Historical context for pattern matching
/// </summary>
public class HistoricalContext
{
    public string ContextId { get; set; } = string.Empty;
    public SystemState SystemState { get; set; } = new();
    public string ApproachUsed { get; set; } = string.Empty;
    public bool WasSuccessful { get; set; }
    public double SuccessScore { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Performance data for learning
/// </summary>
public class PerformanceData
{
    public string DataId { get; set; } = Guid.NewGuid().ToString();
    public PerformanceMetrics Metrics { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Context { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Correlation analysis results
/// </summary>
public class CorrelationAnalysis
{
    public Dictionary<string, double> Correlations { get; set; } = new();
    public IEnumerable<StrongCorrelation> StrongCorrelations { get; set; } = Enumerable.Empty<StrongCorrelation>();
    public IEnumerable<WeakCorrelation> WeakCorrelations { get; set; } = Enumerable.Empty<WeakCorrelation>();
    public double OverallCorrelationStrength { get; set; }
}

/// <summary>
/// Strong correlation between factors
/// </summary>
public class StrongCorrelation
{
    public string Factor1 { get; set; } = string.Empty;
    public string Factor2 { get; set; } = string.Empty;
    public double CorrelationCoefficient { get; set; }
    public string Implication { get; set; } = string.Empty;
}

/// <summary>
/// Weak correlation between factors
/// </summary>
public class WeakCorrelation
{
    public string Factor1 { get; set; } = string.Empty;
    public string Factor2 { get; set; } = string.Empty;
    public double CorrelationCoefficient { get; set; }
    public string Note { get; set; } = string.Empty;
}

/// <summary>
/// Adaptation recommendation
/// </summary>
public class AdaptationRecommendation
{
    public string RecommendationId { get; set; } = Guid.NewGuid().ToString();
    public AdaptationType AdaptationType { get; set; }
    public string Description { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double ExpectedImprovement { get; set; }
    public bool IsSafeToApplyImmediately { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Learning effectiveness metrics
/// </summary>
public class LearningEffectiveness
{
    public double OverallEffectiveness { get; set; }
    public int TotalInsightsGenerated { get; set; }
    public int AppliedInsights { get; set; }
    public double SuccessRate { get; set; }
    public double AverageImprovement { get; set; }
    public DateTime LastLearningCycle { get; set; }
    public Dictionary<string, double> EffectivenessByType { get; set; } = new();
}

// Enums
public enum InsightType
{
    PerformanceOptimization,
    UserExperience,
    PlatformOptimization,
    ResourceOptimization,
    SecurityOptimization,
    ReliabilityOptimization
}

public enum PatternType
{
    PerformanceCorrelation,
    UserSatisfactionCorrelation,
    PlatformSpecificOptimization,
    ResourceUtilizationPattern,
    ErrorPattern,
    SuccessPattern
}
