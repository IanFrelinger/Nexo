using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Adaptation strategy registry interface
/// </summary>
public interface IAdaptationStrategyRegistry
{
    /// <summary>
    /// Register an adaptation strategy
    /// </summary>
    Task RegisterStrategyAsync(string strategyId, object strategy);
    
    /// <summary>
    /// Register an adaptation strategy (synchronous version)
    /// </summary>
    void RegisterStrategy(string strategyId, object strategy);
    
    /// <summary>
    /// Get strategies for adaptation type
    /// </summary>
    Task<IEnumerable<object>> GetStrategiesForAdaptationTypeAsync(AdaptationType adaptationType);
    
    /// <summary>
    /// Get strategies for adaptation type (synchronous version)
    /// </summary>
    IEnumerable<object> GetStrategiesForAdaptationType(AdaptationType adaptationType);
    
    /// <summary>
    /// Get all registered strategies
    /// </summary>
    Task<IEnumerable<object>> GetAllStrategiesAsync();
    
    /// <summary>
    /// Remove a strategy
    /// </summary>
    Task RemoveStrategyAsync(string strategyId);
}