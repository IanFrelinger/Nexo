using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for adaptation learning system
/// </summary>
public interface IAdaptationLearningSystem
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
    /// Record adaptation results
    /// </summary>
    Task RecordAdaptationResultsAsync(AdaptationRecord record, PerformanceMetrics? performanceImpact);
}

/// <summary>
/// Learning statistics
/// </summary>
public record LearningStatistics
{
    /// <summary>
    /// Total number of learning samples
    /// </summary>
    public int TotalSamples { get; init; }
    
    /// <summary>
    /// Number of successful adaptations learned from
    /// </summary>
    public int SuccessfulAdaptations { get; init; }
    
    /// <summary>
    /// Number of failed adaptations learned from
    /// </summary>
    public int FailedAdaptations { get; init; }
    
    /// <summary>
    /// Average learning accuracy
    /// </summary>
    public double AverageAccuracy { get; init; }
    
    /// <summary>
    /// Last learning update timestamp
    /// </summary>
    public DateTime LastUpdate { get; init; }
    
    /// <summary>
    /// Learning model version
    /// </summary>
    public string ModelVersion { get; init; } = string.Empty;
}
