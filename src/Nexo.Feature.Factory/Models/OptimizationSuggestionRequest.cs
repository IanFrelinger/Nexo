using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization suggestion request
/// </summary>
public record OptimizationSuggestionRequest
{
    public List<UsagePattern> UsagePatterns { get; init; } = new();
    public List<PerformanceMetric> PerformanceMetrics { get; init; } = new();
    public string OptimizationType { get; init; } = string.Empty;
    public bool EnableAIOptimization { get; init; }
    public bool EnableManualOptimization { get; init; }
    public Dictionary<string, object> SuggestionParameters { get; init; } = new();
} 