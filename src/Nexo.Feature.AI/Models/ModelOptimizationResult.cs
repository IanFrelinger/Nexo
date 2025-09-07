using System.Collections.Generic;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Result of model optimization
/// </summary>
public record ModelOptimizationResult
{
    /// <summary>
    /// Whether optimization was successful
    /// </summary>
    public bool IsSuccessful { get; init; }
    
    /// <summary>
    /// Model name that was optimized
    /// </summary>
    public string ModelName { get; init; } = string.Empty;
    
    /// <summary>
    /// Performance improvement percentage
    /// </summary>
    public double PerformanceImprovement { get; init; }
    
    /// <summary>
    /// Memory usage reduction percentage
    /// </summary>
    public double MemoryReduction { get; init; }
    
    /// <summary>
    /// Optimization recommendations
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
    
    /// <summary>
    /// Optimization metrics
    /// </summary>
    public Dictionary<string, object> Metrics { get; init; } = new();
    
    /// <summary>
    /// Error message if optimization failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}
