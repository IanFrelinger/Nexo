using System;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Trend data point
/// </summary>
public record TrendDataPoint
{
    public DateTime Date { get; init; }
    public double ProductivityMultiplier { get; init; }
    public int FeatureCount { get; init; }
    public TimeSpan AverageTime { get; init; }
    public double QualityScore { get; init; }
    public double CostSavings { get; init; }
} 