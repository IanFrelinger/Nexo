using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity metrics result
/// </summary>
public record ProductivityMetricsResult
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public double ProductivityMultiplier { get; init; }
    public TimeSpan AverageTraditionalTime { get; init; }
    public TimeSpan AverageFeatureFactoryTime { get; init; }
    public double TimeSavingsPercentage { get; init; }
    public double CostSavingsPercentage { get; init; }
    public int TotalFeatures { get; init; }
    public int TraditionalFeatures { get; init; }
    public int FeatureFactoryFeatures { get; init; }
    public Dictionary<string, double> DetailedMetrics { get; init; } = new();
    public List<ProductivityDataPoint> DataPoints { get; init; } = new();
} 