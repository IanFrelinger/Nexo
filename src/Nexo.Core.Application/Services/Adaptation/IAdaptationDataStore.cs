using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;

namespace Nexo.Core.Application.Services.Adaptation;

/// <summary>
/// Interface for storing adaptation data
/// </summary>
public interface IAdaptationDataStore
{
    /// <summary>
    /// Store adaptation record
    /// </summary>
    Task StoreAdaptationAsync(AdaptationRecord adaptation);
    
    /// <summary>
    /// Get adaptation by ID
    /// </summary>
    Task<AdaptationRecord?> GetAdaptationAsync(string id);
    
    /// <summary>
    /// Get adaptations within time range
    /// </summary>
    Task<IEnumerable<AdaptationRecord>> GetAdaptationsAsync(DateTime startTime, DateTime endTime);
    
    /// <summary>
    /// Get recent adaptations
    /// </summary>
    Task<IEnumerable<AdaptationRecord>> GetRecentAdaptationsAsync(int count = 10);
    
    /// <summary>
    /// Store adaptation improvement
    /// </summary>
    Task StoreImprovementAsync(AdaptationImprovement improvement);
    
    /// <summary>
    /// Get improvements for adaptation
    /// </summary>
    Task<IEnumerable<AdaptationImprovement>> GetImprovementsAsync(string adaptationId);
    
    /// <summary>
    /// Store learning insight
    /// </summary>
    Task StoreInsightAsync(LearningInsight insight);
    
    /// <summary>
    /// Get learning insights
    /// </summary>
    Task<IEnumerable<LearningInsight>> GetInsightsAsync();
    
    /// <summary>
    /// Get active adaptations
    /// </summary>
    Task<IEnumerable<AdaptationRecord>> GetActiveAdaptationsAsync();
    
    /// <summary>
    /// Get recent improvements
    /// </summary>
    Task<IEnumerable<AdaptationImprovement>> GetRecentImprovementsAsync(int count = 10);
    
    /// <summary>
    /// Get total adaptations count
    /// </summary>
    Task<int> GetTotalAdaptationsCountAsync();
    
    /// <summary>
    /// Get overall effectiveness
    /// </summary>
    Task<double> GetOverallEffectivenessAsync();
    
    /// <summary>
    /// Delete old data
    /// </summary>
    Task DeleteOldDataAsync(DateTime cutoffTime);
}
