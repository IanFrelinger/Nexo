using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for continuous learning system
/// </summary>
public interface IContinuousLearningSystem
{
    /// <summary>
    /// Learn from adaptation results
    /// </summary>
    Task LearnFromAdaptationAsync(AdaptationRecord record, PerformanceMetrics? performanceImpact);
    
    /// <summary>
    /// Learn from user feedback
    /// </summary>
    Task LearnFromFeedbackAsync(UserFeedback feedback);
    
    /// <summary>
    /// Learn from performance data
    /// </summary>
    Task LearnFromPerformanceDataAsync(IEnumerable<PerformanceData> performanceData);
    
    /// <summary>
    /// Learn from system state changes
    /// </summary>
    Task LearnFromSystemStateAsync(SystemState systemState);
    
    /// <summary>
    /// Learn from environment changes
    /// </summary>
    Task LearnFromEnvironmentChangeAsync(EnvironmentChange environmentChange);
    
    /// <summary>
    /// Generate learning insights
    /// </summary>
    Task<IEnumerable<LearningInsight>> GenerateInsightsAsync();
    
    /// <summary>
    /// Get learning recommendations
    /// </summary>
    Task<IEnumerable<AdaptationNeed>> GetLearningRecommendationsAsync();
    
    /// <summary>
    /// Update learning model
    /// </summary>
    Task UpdateLearningModelAsync();
    
    /// <summary>
    /// Get learning statistics
    /// </summary>
    Task<LearningStatistics> GetLearningStatisticsAsync();
    
    /// <summary>
    /// Get learning predictions
    /// </summary>
    Task<IEnumerable<LearningPrediction>> GetLearningPredictionsAsync();
    
    /// <summary>
    /// Evaluate learning effectiveness
    /// </summary>
    Task<LearningEffectiveness> EvaluateLearningEffectivenessAsync();
    
    /// <summary>
    /// Get learning history
    /// </summary>
    Task<IEnumerable<LearningRecord>> GetLearningHistoryAsync(TimeSpan? timeWindow = null);
    
    /// <summary>
    /// Clear learning data
    /// </summary>
    Task ClearLearningDataAsync();
    
    /// <summary>
    /// Export learning data
    /// </summary>
    Task<byte[]> ExportLearningDataAsync();
    
    /// <summary>
    /// Import learning data
    /// </summary>
    Task ImportLearningDataAsync(byte[] data);
}

/// <summary>
/// Learning prediction
/// </summary>
public record LearningPrediction
{
    /// <summary>
    /// Prediction identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Prediction type
    /// </summary>
    public LearningPredictionType Type { get; init; }
    
    /// <summary>
    /// Predicted value
    /// </summary>
    public double PredictedValue { get; init; }
    
    /// <summary>
    /// Prediction confidence (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Prediction timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Prediction window
    /// </summary>
    public TimeSpan PredictionWindow { get; init; }
    
    /// <summary>
    /// Prediction method
    /// </summary>
    public string Method { get; init; } = string.Empty;
    
    /// <summary>
    /// Prediction context
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
}

/// <summary>
/// Learning prediction types
/// </summary>
public enum LearningPredictionType
{
    /// <summary>
    /// Performance prediction
    /// </summary>
    Performance,
    
    /// <summary>
    /// User behavior prediction
    /// </summary>
    UserBehavior,
    
    /// <summary>
    /// System load prediction
    /// </summary>
    SystemLoad,
    
    /// <summary>
    /// Error prediction
    /// </summary>
    Error,
    
    /// <summary>
    /// Resource usage prediction
    /// </summary>
    ResourceUsage
}

/// <summary>
/// Learning effectiveness assessment
/// </summary>
public record LearningEffectiveness
{
    /// <summary>
    /// Overall effectiveness score (0-100)
    /// </summary>
    public double OverallScore { get; init; }
    
    /// <summary>
    /// Accuracy score (0-100)
    /// </summary>
    public double AccuracyScore { get; init; }
    
    /// <summary>
    /// Prediction accuracy (0-100)
    /// </summary>
    public double PredictionAccuracy { get; init; }
    
    /// <summary>
    /// Learning speed score (0-100)
    /// </summary>
    public double LearningSpeedScore { get; init; }
    
    /// <summary>
    /// Adaptation effectiveness (0-100)
    /// </summary>
    public double AdaptationEffectiveness { get; init; }
    
    /// <summary>
    /// User satisfaction score (0-100)
    /// </summary>
    public double UserSatisfactionScore { get; init; }
    
    /// <summary>
    /// Performance improvement (0-100)
    /// </summary>
    public double PerformanceImprovement { get; init; }
    
    /// <summary>
    /// Learning efficiency (0-100)
    /// </summary>
    public double LearningEfficiency { get; init; }
    
    /// <summary>
    /// Assessment timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Assessment period
    /// </summary>
    public TimeSpan AssessmentPeriod { get; init; }
    
    /// <summary>
    /// Key strengths
    /// </summary>
    public List<string> KeyStrengths { get; init; } = new();
    
    /// <summary>
    /// Areas for improvement
    /// </summary>
    public List<string> AreasForImprovement { get; init; } = new();
    
    /// <summary>
    /// Recommendations
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
}

/// <summary>
/// Learning record
/// </summary>
public record LearningRecord
{
    /// <summary>
    /// Record identifier
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Learning type
    /// </summary>
    public LearningType Type { get; init; }
    
    /// <summary>
    /// Learning data
    /// </summary>
    public object Data { get; init; } = new();
    
    /// <summary>
    /// Learning outcome
    /// </summary>
    public LearningOutcome Outcome { get; init; }
    
    /// <summary>
    /// Learning timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Learning duration
    /// </summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>
    /// Learning success
    /// </summary>
    public bool WasSuccessful { get; init; }
    
    /// <summary>
    /// Learning confidence (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Learning context
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
}

/// <summary>
/// Learning types
/// </summary>
public enum LearningType
{
    /// <summary>
    /// Adaptation learning
    /// </summary>
    Adaptation,
    
    /// <summary>
    /// Feedback learning
    /// </summary>
    Feedback,
    
    /// <summary>
    /// Performance learning
    /// </summary>
    Performance,
    
    /// <summary>
    /// System state learning
    /// </summary>
    SystemState,
    
    /// <summary>
    /// Environment learning
    /// </summary>
    Environment,
    
    /// <summary>
    /// User behavior learning
    /// </summary>
    UserBehavior
}

/// <summary>
/// Learning outcomes
/// </summary>
public enum LearningOutcome
{
    /// <summary>
    /// Successful learning
    /// </summary>
    Success,
    
    /// <summary>
    /// Partial learning
    /// </summary>
    Partial,
    
    /// <summary>
    /// Failed learning
    /// </summary>
    Failure,
    
    /// <summary>
    /// Inconclusive learning
    /// </summary>
    Inconclusive
}