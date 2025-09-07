using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Performance recommendation request
/// </summary>
public record PerformanceRecommendationRequest
{
    public List<PerformanceMetric> CurrentMetrics { get; init; } = new();
    public List<PerformanceTarget> PerformanceTargets { get; init; } = new();
    public string RecommendationType { get; init; } = string.Empty;
    public bool EnableAutomatedOptimization { get; init; }
    public Dictionary<string, object> RecommendationParameters { get; init; } = new();
} 