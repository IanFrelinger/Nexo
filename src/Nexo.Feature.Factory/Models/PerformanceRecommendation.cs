using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Performance recommendation
/// </summary>
public record PerformanceRecommendation
{
    public string RecommendationId { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public double CurrentValue { get; init; }
    public double TargetValue { get; init; }
    public double ImprovementPotential { get; init; }
    public string Recommendation { get; init; } = string.Empty;
    public List<string> Actions { get; init; } = new();
    public double Priority { get; init; }
    public string Effort { get; init; } = string.Empty;
    public Dictionary<string, object> RecommendationData { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
} 