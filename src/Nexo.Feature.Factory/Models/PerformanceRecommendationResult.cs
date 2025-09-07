using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Performance recommendation result
/// </summary>
public record PerformanceRecommendationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<PerformanceRecommendation> Recommendations { get; init; } = new();
    public int RecommendationsGenerated { get; init; }
    public double OverallImprovementPotential { get; init; }
    public TimeSpan GenerationDuration { get; init; }
    public Dictionary<string, double> PerformanceMetrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 