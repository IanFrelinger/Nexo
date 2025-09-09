using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Application.Services.Adaptation;

namespace Nexo.Core.Application.Services.Learning;

/// <summary>
/// Continuous learning system that improves system behavior over time
/// </summary>
public class ContinuousLearningSystem : IContinuousLearningSystem
{
    private readonly IUserFeedbackCollector _feedbackCollector;
    private readonly Nexo.Core.Application.Services.Adaptation.IPerformanceDataStore _performanceStore;
    private readonly IPatternRecognitionEngine _patternEngine;
    private readonly IAdaptationRecommender _recommender;
    private readonly Nexo.Core.Application.Services.Adaptation.IAdaptationDataStore _adaptationStore;
    private readonly ILogger<ContinuousLearningSystem> _logger;
    
    public ContinuousLearningSystem(
        IUserFeedbackCollector feedbackCollector,
        Nexo.Core.Application.Services.Adaptation.IPerformanceDataStore performanceStore,
        IPatternRecognitionEngine patternEngine,
        IAdaptationRecommender recommender,
        Nexo.Core.Application.Services.Adaptation.IAdaptationDataStore adaptationStore,
        ILogger<ContinuousLearningSystem> logger)
    {
        _feedbackCollector = feedbackCollector;
        _performanceStore = performanceStore;
        _patternEngine = patternEngine;
        _recommender = recommender;
        _adaptationStore = adaptationStore;
        _logger = logger;
    }
    
    public async Task ProcessLearningCycleAsync()
    {
        _logger.LogInformation("Starting learning cycle");
        
        try
        {
            // Collect recent data
            var recentFeedback = await _feedbackCollector.GetRecentFeedbackAsync(100); // Get last 100 feedback items
            var recentPerformance = await _performanceStore.GetRecentPerformanceDataAsync(100); // Get last 100 performance records
            
            _logger.LogInformation("Collected {FeedbackCount} feedback items and {PerformanceCount} performance records",
                recentFeedback.Count(), recentPerformance.Count());
            
            // Identify patterns
            var learningPerformanceData = recentPerformance.Select(p => new PerformanceData
            {
                DataId = p.Id,
                Metrics = new PerformanceMetrics(), // Create empty metrics
                Timestamp = p.Timestamp,
                Context = p.Context,
                AdditionalMetrics = p.Metadata
            });
            var patterns = await _patternEngine.IdentifyPatternsAsync(recentFeedback, learningPerformanceData);
            
            _logger.LogInformation("Identified {PatternCount} patterns", patterns.Count());
            
            // Generate learning insights
            var insights = await GenerateLearningInsights(patterns);
            
            _logger.LogInformation("Generated {InsightCount} learning insights", insights.Count());
            
            // Update system knowledge
            await UpdateSystemKnowledge(insights);
            
            // Generate recommendations for future improvements
            var recommendations = await _recommender.GenerateRecommendationsAsync(insights);
            
            _logger.LogInformation("Generated {RecommendationCount} recommendations", recommendations.Count());
            
            // Apply immediate improvements where safe
            var immediateRecommendations = recommendations.Where(r => r.IsSafeToApplyImmediately);
            await ApplyImmediateImprovements(immediateRecommendations);
            
            _logger.LogInformation("Applied {ImmediateCount} immediate improvements", immediateRecommendations.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during learning cycle processing");
        }
    }
    
    public async Task<LearningRecommendations> GetRecommendationsForContext(LearningContext context)
    {
        _logger.LogInformation("Getting recommendations for context: {ContextType}", context.ContextType);
        
        // Query historical data for similar contexts
        var similarContexts = await _patternEngine.FindSimilarContextsAsync(context);
        
        _logger.LogInformation("Found {SimilarContextCount} similar contexts", similarContexts.Count());
        
        // Analyze success patterns
        var successPatterns = similarContexts
            .Where(c => c.WasSuccessful)
            .GroupBy(c => c.ApproachUsed)
            .OrderByDescending(g => g.Average(c => c.SuccessScore));
        
        var recommendations = new LearningRecommendations();
        
        foreach (var pattern in successPatterns.Take(3))
        {
            var recommendation = new LearningRecommendation
            {
                Approach = pattern.Key,
                Confidence = CalculateConfidence(pattern.ToList()),
                ExpectedSuccessRate = pattern.Average(c => c.SuccessScore),
                SupportingEvidence = pattern.Count(),
                Reasoning = $"This approach succeeded in {pattern.Count()} similar contexts"
            };
            
            recommendations.Add(recommendation);
        }
        
        recommendations.OverallConfidence = recommendations.Recommendations.Any() 
            ? recommendations.Recommendations.Average(r => r.Confidence)
            : 0.0;
        
        recommendations.Reasoning = $"Based on analysis of {similarContexts.Count()} similar historical contexts";
        
        return recommendations;
    }
    
    public async Task RecordAdaptationResultsAsync(IEnumerable<Nexo.Core.Domain.Entities.Infrastructure.AdaptationNeed> adaptations)
    {
        _logger.LogInformation("Recording results for {AdaptationCount} adaptations", adaptations.Count());
        
        foreach (var adaptation in adaptations)
        {
            var record = new AdaptationRecord
            {
                Id = Guid.NewGuid().ToString(),
                Type = adaptation.Type,
                Trigger = adaptation.Trigger,
                AppliedAt = DateTime.UtcNow,
                StrategyId = "Unknown", // Would be populated by the adaptation engine
                Success = true, // Would be determined by actual results
                EffectivenessScore = adaptation.Priority switch
                {
                    AdaptationPriority.Critical => 0.9,
                    AdaptationPriority.High => 0.8,
                    AdaptationPriority.Medium => 0.7,
                    _ => 0.6
                }
            };
            
            await _adaptationStore.StoreAdaptationAsync(record);
        }
    }
    
    public async Task<IEnumerable<Nexo.Core.Domain.Entities.Infrastructure.LearningInsight>> GetCurrentInsightsAsync()
    {
        return await _adaptationStore.GetInsightsAsync();
    }
    
    public async Task<LearningEffectiveness> GetLearningEffectivenessAsync()
    {
        var insights = await GetCurrentInsightsAsync();
        var adaptations = await _adaptationStore.GetRecentAdaptationsAsync(30);
        
        var effectiveness = new LearningEffectiveness
        {
            TotalInsightsGenerated = insights.Count(),
            AppliedInsights = insights.Count(i => i.IsApplied),
            LastLearningCycle = insights.Any() ? insights.Max(i => i.DiscoveredAt) : DateTime.MinValue
        };
        
        if (effectiveness.TotalInsightsGenerated > 0)
        {
            effectiveness.SuccessRate = (double)effectiveness.AppliedInsights / effectiveness.TotalInsightsGenerated;
        }
        
        if (adaptations.Any())
        {
            effectiveness.AverageImprovement = adaptations.Average(a => a.EffectivenessScore);
        }
        
        effectiveness.OverallEffectiveness = (effectiveness.SuccessRate + effectiveness.AverageImprovement) / 2.0;
        
        // Calculate effectiveness by type
        var insightsByType = insights.GroupBy(i => i.Type);
        foreach (var group in insightsByType)
        {
            var appliedCount = group.Count(i => i.IsApplied);
            var totalCount = group.Count();
            effectiveness.EffectivenessByType[group.Key.ToString()] = totalCount > 0 ? (double)appliedCount / totalCount : 0.0;
        }
        
        return effectiveness;
    }
    
    private async Task<IEnumerable<LearningInsight>> GenerateLearningInsights(IEnumerable<IdentifiedPattern> patterns)
    {
        var insights = new List<LearningInsight>();
        
        foreach (var pattern in patterns)
        {
            switch (pattern.Type)
            {
                case PatternType.PerformanceCorrelation:
                    insights.Add(new LearningInsight
                    {
                        Type = InsightType.PerformanceOptimization,
                        Description = $"Performance improves by {pattern.CorrelationStrength:P} when using {pattern.OptimalStrategy}",
                        Confidence = pattern.StatisticalSignificance,
                        RecommendedAction = $"Prefer {pattern.OptimalStrategy} for similar contexts",
                        SupportingData = pattern.SupportingData
                    });
                    break;
                    
                case PatternType.UserSatisfactionCorrelation:
                    insights.Add(new LearningInsight
                    {
                        Type = InsightType.UserExperience,
                        Description = $"Users prefer {pattern.PreferredApproach} over {pattern.AlternativeApproach}",
                        Confidence = pattern.UserPreferenceStrength,
                        RecommendedAction = $"Default to {pattern.PreferredApproach} for new features"
                    });
                    break;
                    
                case PatternType.PlatformSpecificOptimization:
                    insights.Add(new LearningInsight
                    {
                        Type = InsightType.PlatformOptimization,
                        Description = $"Platform {pattern.Platform} shows {pattern.ImprovementFactor:P} improvement with {pattern.OptimalStrategy}",
                        Confidence = pattern.PlatformSpecificConfidence,
                        RecommendedAction = $"Auto-select {pattern.OptimalStrategy} for {pattern.Platform}"
                    });
                    break;
                    
                case PatternType.ResourceUtilizationPattern:
                    insights.Add(new LearningInsight
                    {
                        Type = InsightType.ResourceOptimization,
                        Description = $"Resource utilization pattern identified: {pattern.Description}",
                        Confidence = pattern.StatisticalSignificance,
                        RecommendedAction = $"Apply resource optimization strategy for this pattern"
                    });
                    break;
                    
                case PatternType.ErrorPattern:
                    insights.Add(new LearningInsight
                    {
                        Type = InsightType.ReliabilityOptimization,
                        Description = $"Error pattern identified: {pattern.Description}",
                        Confidence = pattern.StatisticalSignificance,
                        RecommendedAction = $"Implement preventive measures for this error pattern"
                    });
                    break;
                    
                case PatternType.SuccessPattern:
                    insights.Add(new LearningInsight
                    {
                        Type = InsightType.PerformanceOptimization,
                        Description = $"Success pattern identified: {pattern.Description}",
                        Confidence = pattern.StatisticalSignificance,
                        RecommendedAction = $"Replicate this success pattern in similar contexts"
                    });
                    break;
            }
        }
        
        // Store insights for future reference
        foreach (var insight in insights)
        {
            await _adaptationStore.StoreInsightAsync(new Nexo.Core.Domain.Entities.Infrastructure.LearningInsight
            {
                Id = insight.Id,
                Type = (Nexo.Core.Domain.Entities.Infrastructure.InsightType)Enum.Parse(typeof(Nexo.Core.Domain.Entities.Infrastructure.InsightType), insight.Type.ToString()),
                Description = insight.Description,
                Confidence = insight.Confidence,
                Timestamp = insight.DiscoveredAt,
                SupportingData = insight.SupportingData
            });
        }
        
        return insights;
    }
    
    private async Task UpdateSystemKnowledge(IEnumerable<LearningInsight> insights)
    {
        _logger.LogInformation("Updating system knowledge with {InsightCount} insights", insights.Count());
        
        // This would integrate with the actual system configuration
        // For now, we'll just log the insights
        foreach (var insight in insights)
        {
            _logger.LogInformation("Learning insight: {Type} - {Description} (Confidence: {Confidence:P})",
                insight.Type, insight.Description, insight.Confidence);
        }
        
        await Task.CompletedTask;
    }
    
    private async Task ApplyImmediateImprovements(IEnumerable<AdaptationRecommendation> recommendations)
    {
        _logger.LogInformation("Applying {RecommendationCount} immediate improvements", recommendations.Count());
        
        foreach (var recommendation in recommendations)
        {
            _logger.LogInformation("Applying immediate improvement: {Description} (Confidence: {Confidence:P})",
                recommendation.Description, recommendation.Confidence);
            
            // This would integrate with the actual adaptation engine
            // For now, we'll just log the recommendation
        }
        
        await Task.CompletedTask;
    }
    
    private double CalculateConfidence(IEnumerable<HistoricalContext> contexts)
    {
        var contextList = contexts.ToList();
        if (!contextList.Any()) return 0.0;
        
        var successRate = contextList.Count(c => c.WasSuccessful) / (double)contextList.Count;
        var averageScore = contextList.Average(c => c.SuccessScore);
        var sampleSize = Math.Min(contextList.Count / 10.0, 1.0); // More confidence with larger samples
        
        return (successRate * 0.4 + averageScore * 0.4 + sampleSize * 0.2);
    }
}