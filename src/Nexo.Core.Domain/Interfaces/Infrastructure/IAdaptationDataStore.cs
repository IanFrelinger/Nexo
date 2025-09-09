using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Adaptation data store interface
/// </summary>
public interface IAdaptationDataStore
{
    /// <summary>
    /// Record an adaptation
    /// </summary>
    Task RecordAdaptationAsync(AdaptationRecord adaptation);
    
    /// <summary>
    /// Record applied adaptation
    /// </summary>
    Task RecordAppliedAdaptationAsync(AdaptationRecord adaptation);
    
    /// <summary>
    /// Get recent adaptations
    /// </summary>
    Task<IEnumerable<AdaptationRecord>> GetRecentAdaptationsAsync(TimeSpan timeSpan);
    
    /// <summary>
    /// Get recent insights
    /// </summary>
    Task<IEnumerable<LearningInsight>> GetRecentInsightsAsync(TimeSpan timeSpan);
    
    /// <summary>
    /// Store insight
    /// </summary>
    Task StoreInsightAsync(LearningInsight insight);
    
    /// <summary>
    /// Get adaptations by type
    /// </summary>
    Task<IEnumerable<AdaptationRecord>> GetAdaptationsByTypeAsync(AdaptationType type);
    
    /// <summary>
    /// Get active adaptations
    /// </summary>
    Task<IEnumerable<AdaptationRecord>> GetActiveAdaptationsAsync();
    
    /// <summary>
    /// Get recent improvements
    /// </summary>
    Task<IEnumerable<AdaptationRecord>> GetRecentImprovementsAsync(TimeSpan timeSpan);
    
    /// <summary>
    /// Get total adaptations count
    /// </summary>
    Task<int> GetTotalAdaptationsCountAsync();
    
    /// <summary>
    /// Get overall effectiveness
    /// </summary>
    Task<double> GetOverallEffectivenessAsync();
}