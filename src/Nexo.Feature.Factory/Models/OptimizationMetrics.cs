using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization metrics
/// </summary>
public record OptimizationMetrics
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public double OverallOptimizationScore { get; init; }
    public int TotalSuggestions { get; init; }
    public int ImplementedSuggestions { get; init; }
    public double ImplementationRate { get; init; }
    public double AverageImprovement { get; init; }
    public List<OptimizationTrend> OptimizationTrends { get; init; } = new();
    public Dictionary<string, double> DetailedMetrics { get; init; } = new();
    public DateTime GeneratedAt { get; init; }
} 