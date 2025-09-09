using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Interface for analyzing user experience
/// </summary>
public interface IUserExperienceAnalyzer
{
    /// <summary>
    /// Analyze user experience metrics
    /// </summary>
    Task<Nexo.Core.Domain.Entities.Infrastructure.UserExperienceAnalysis> AnalyzeUserExperienceAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get user experience score
    /// </summary>
    Task<double> GetUserExperienceScoreAsync();
    
    /// <summary>
    /// Get user experience trends
    /// </summary>
    Task<IEnumerable<Nexo.Core.Domain.Entities.Infrastructure.UserExperienceTrend>> GetUserExperienceTrendsAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get user experience recommendations
    /// </summary>
    Task<IEnumerable<string>> GetUserExperienceRecommendationsAsync();
    
    /// <summary>
    /// Analyze user feedback
    /// </summary>
    Task<Nexo.Core.Domain.Entities.Infrastructure.FeedbackAnalysisResult> AnalyzeUserFeedbackAsync(IEnumerable<UserFeedback> feedback);
    
    /// <summary>
    /// Get user experience insights
    /// </summary>
    Task<IEnumerable<Nexo.Core.Domain.Entities.Infrastructure.LearningInsight>> GetUserExperienceInsightsAsync();
    
    /// <summary>
    /// Optimize user experience
    /// </summary>
    Task<OptimizationResult> OptimizeUserExperienceAsync();
    
    /// <summary>
    /// Get user experience metrics
    /// </summary>
    Task<UserExperienceMetrics> GetUserExperienceMetricsAsync();
}
