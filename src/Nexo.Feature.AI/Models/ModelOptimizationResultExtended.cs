using System;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Extended result of AI model optimization
/// </summary>
public record ModelOptimizationResultExtended : ModelOptimizationResult
{
    /// <summary>
    /// Whether the optimization was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Performance analysis details
    /// </summary>
    public string PerformanceAnalysis { get; init; } = string.Empty;
    
    /// <summary>
    /// Identified bottlenecks
    /// </summary>
    public List<string> Bottlenecks { get; init; } = new();
    
    /// <summary>
    /// When the analysis was performed
    /// </summary>
    public DateTime AnalysisTimestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
