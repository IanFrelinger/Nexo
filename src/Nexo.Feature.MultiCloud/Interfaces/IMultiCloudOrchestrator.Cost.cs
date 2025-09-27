using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.MultiCloud.Interfaces;

/// <summary>
/// Multi-cloud cost analysis and optimization capabilities
/// </summary>
public partial interface IMultiCloudOrchestrator
{
    /// <summary>
    /// Gets cost analysis across all cloud providers
    /// </summary>
    /// <param name="startDate">Start date for cost analysis</param>
    /// <param name="endDate">End date for cost analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cost analysis across all providers</returns>
    Task<MultiCloudCostAnalysis> GetCostAnalysisAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes costs across cloud providers
    /// </summary>
    /// <param name="optimizationRequest">Cost optimization request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cost optimization recommendations</returns>
    Task<MultiCloudCostOptimization> OptimizeCostsAsync(MultiCloudCostOptimizationRequest optimizationRequest, CancellationToken cancellationToken = default);
}

/// <summary>
/// Multi-cloud cost analysis
/// </summary>
public record MultiCloudCostAnalysis
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal TotalCost { get; init; }
    public string Currency { get; init; } = string.Empty;
    public List<ProviderCostBreakdown> ProviderCosts { get; init; } = new();
    public Dictionary<string, decimal> ServiceCosts { get; init; } = new();
    public List<CostOptimizationRecommendation> Recommendations { get; init; } = new();
}

/// <summary>
/// Provider cost breakdown
/// </summary>
public record ProviderCostBreakdown
{
    public string ProviderName { get; init; } = string.Empty;
    public decimal Cost { get; init; }
    public string Currency { get; init; } = string.Empty;
    public Dictionary<string, decimal> ServiceCosts { get; init; } = new();
    public List<CostTrend> Trends { get; init; } = new();
}

/// <summary>
/// Cost trend
/// </summary>
public record CostTrend
{
    public DateTime Date { get; init; }
    public decimal Cost { get; init; }
    public decimal Change { get; init; }
    public double PercentageChange { get; init; }
}

/// <summary>
/// Cost optimization recommendation
/// </summary>
public record CostOptimizationRecommendation
{
    public string ProviderName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
    public decimal PotentialSavings { get; init; }
    public string Impact { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
}

/// <summary>
/// Multi-cloud cost optimization request
/// </summary>
public record MultiCloudCostOptimizationRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public List<string> TargetProviders { get; init; } = new();
    public OptimizationStrategy Strategy { get; init; }
    public Dictionary<string, object> Constraints { get; init; } = new();
}

/// <summary>
/// Optimization strategy
/// </summary>
public enum OptimizationStrategy
{
    Aggressive,
    Conservative,
    Balanced,
    Custom
}

/// <summary>
/// Multi-cloud cost optimization
/// </summary>
public record MultiCloudCostOptimization
{
    public string OptimizationId { get; init; } = string.Empty;
    public DateTime OptimizedAt { get; init; }
    public decimal CurrentCost { get; init; }
    public decimal OptimizedCost { get; init; }
    public decimal PotentialSavings { get; init; }
    public List<CostOptimizationRecommendation> Recommendations { get; init; } = new();
    public List<OptimizationAction> Actions { get; init; } = new();
}

/// <summary>
/// Optimization action
/// </summary>
public record OptimizationAction
{
    public string Action { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public decimal Savings { get; init; }
    public string Impact { get; init; } = string.Empty;
    public bool IsAutomatic { get; init; }
}
