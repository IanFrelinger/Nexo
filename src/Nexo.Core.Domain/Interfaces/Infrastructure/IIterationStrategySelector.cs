using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for iteration strategy selection
/// </summary>
public interface IIterationStrategySelector
{
    /// <summary>
    /// Select the optimal iteration strategy for the given context
    /// </summary>
    /// <typeparam name="T">Type of items to iterate over</typeparam>
    /// <param name="context">Iteration context</param>
    /// <returns>Optimal iteration strategy</returns>
    IIterationStrategy<T> SelectStrategy<T>(IterationContext context);
    
    /// <summary>
    /// Select the optimal iteration strategy for the given source and requirements
    /// </summary>
    /// <typeparam name="T">Type of items to iterate over</typeparam>
    /// <param name="source">Source collection</param>
    /// <param name="requirements">Iteration requirements</param>
    /// <returns>Optimal iteration strategy</returns>
    IIterationStrategy<T> SelectStrategy<T>(IEnumerable<T> source, IterationRequirements requirements);
    
    /// <summary>
    /// Register a new iteration strategy
    /// </summary>
    /// <typeparam name="T">Type of items to iterate over</typeparam>
    /// <param name="strategy">Strategy to register</param>
    void RegisterStrategy<T>(IIterationStrategy<T> strategy);
    
    /// <summary>
    /// Set the runtime environment profile
    /// </summary>
    /// <param name="profile">Environment profile</param>
    void SetEnvironmentProfile(RuntimeEnvironmentProfile profile);
    
    /// <summary>
    /// Get all available strategies for the given context
    /// </summary>
    /// <typeparam name="T">Type of items to iterate over</typeparam>
    /// <param name="context">Iteration context</param>
    /// <returns>Available strategies ordered by suitability</returns>
    IEnumerable<IIterationStrategy<T>> GetAvailableStrategies<T>(IterationContext context);
    
    /// <summary>
    /// Get selection reasoning for a strategy choice
    /// </summary>
    /// <param name="context">Iteration context</param>
    /// <returns>Human-readable reasoning for strategy selection</returns>
    string GetSelectionReasoning(IterationContext context);
    
    /// <summary>
    /// Estimate performance for a strategy in the given context
    /// </summary>
    /// <param name="strategy">Strategy to estimate</param>
    /// <param name="context">Iteration context</param>
    /// <returns>Performance estimate</returns>
    Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate EstimatePerformance(IIterationStrategy<object> strategy, IterationContext context);
    
    /// <summary>
    /// Compare multiple strategies for the given context
    /// </summary>
    /// <typeparam name="T">Type of items to iterate over</typeparam>
    /// <param name="context">Iteration context</param>
    /// <returns>Strategy comparison results</returns>
    Task<IEnumerable<StrategyComparisonResult>> CompareStrategies<T>(IterationContext context);
    
    /// <summary>
    /// Get strategy recommendations for different scenarios
    /// </summary>
    /// <param name="platform">Target platform</param>
    /// <returns>Strategy recommendations</returns>
    IEnumerable<StrategyRecommendation> GetRecommendations(Entities.Iteration.PlatformTarget platform);
}