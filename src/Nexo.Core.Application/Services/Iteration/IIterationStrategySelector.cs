using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Application.Services.Iteration;

/// <summary>
/// Service for selecting optimal iteration strategies based on context
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
    PerformanceEstimate EstimatePerformance(IIterationStrategy<object> strategy, IterationContext context);
    
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
    IEnumerable<StrategyRecommendation> GetRecommendations(PlatformType platform);
}

/// <summary>
/// Strategy comparison result
/// </summary>
public record StrategyComparisonResult
{
    /// <summary>
    /// Strategy being compared
    /// </summary>
    public IIterationStrategy<object> Strategy { get; init; } = null!;
    
    /// <summary>
    /// Performance estimate
    /// </summary>
    public PerformanceEstimate PerformanceEstimate { get; init; } = new();
    
    /// <summary>
    /// Suitability score (0-100)
    /// </summary>
    public double SuitabilityScore { get; init; }
    
    /// <summary>
    /// Reasoning for this score
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether this strategy is recommended
    /// </summary>
    public bool IsRecommended { get; init; }
}

/// <summary>
/// Strategy recommendation for a platform
/// </summary>
public record StrategyRecommendation
{
    /// <summary>
    /// Scenario description
    /// </summary>
    public string Scenario { get; init; } = string.Empty;
    
    /// <summary>
    /// Recommended strategy ID
    /// </summary>
    public string RecommendedStrategyId { get; init; } = string.Empty;
    
    /// <summary>
    /// Reasoning for the recommendation
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;
    
    /// <summary>
    /// Data size range where this recommendation applies
    /// </summary>
    public (int Min, int Max) DataSizeRange { get; init; }
    
    /// <summary>
    /// Performance characteristics
    /// </summary>
    public string PerformanceCharacteristics { get; init; } = string.Empty;
}
