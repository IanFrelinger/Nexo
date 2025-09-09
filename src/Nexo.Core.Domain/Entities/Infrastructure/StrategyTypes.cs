using System;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Strategy comparison result
/// </summary>
public record StrategyComparisonResult
{
    /// <summary>
    /// The strategy being compared
    /// </summary>
    public IIterationStrategy<object> Strategy { get; init; } = null!;
    
    /// <summary>
    /// Performance estimate for this strategy
    /// </summary>
    public PerformanceEstimate PerformanceEstimate { get; init; } = new();
    
    /// <summary>
    /// Suitability score (0-100)
    /// </summary>
    public double SuitabilityScore { get; init; }
    
    /// <summary>
    /// Reasoning for the score
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether this strategy is recommended
    /// </summary>
    public bool IsRecommended { get; init; }
    
    /// <summary>
    /// Additional comparison data
    /// </summary>
    public Dictionary<string, object> ComparisonData { get; init; } = new();
}

/// <summary>
/// Strategy recommendation
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
    /// Data size range for this recommendation
    /// </summary>
    public (int Min, int Max) DataSizeRange { get; init; }
    
    /// <summary>
    /// Performance characteristics
    /// </summary>
    public string PerformanceCharacteristics { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence level (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Additional recommendation data
    /// </summary>
    public Dictionary<string, object> RecommendationData { get; init; } = new();
}

/// <summary>
/// Performance estimate
/// </summary>
public record PerformanceEstimate
{
    /// <summary>
    /// Estimated execution time in milliseconds
    /// </summary>
    public double EstimatedExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Estimated memory usage in MB
    /// </summary>
    public double EstimatedMemoryUsageMB { get; init; }
    
    /// <summary>
    /// Confidence level (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Performance score (0-100)
    /// </summary>
    public double PerformanceScore { get; init; }
    
    /// <summary>
    /// Whether the estimate meets requirements
    /// </summary>
    public bool MeetsRequirements { get; init; }
    
    /// <summary>
    /// Additional performance metrics
    /// </summary>
    public Dictionary<string, object> Metrics { get; init; } = new();
}
