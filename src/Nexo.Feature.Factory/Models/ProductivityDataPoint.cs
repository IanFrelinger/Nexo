using System;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity data point
/// </summary>
public record ProductivityDataPoint
{
    public DateTime Date { get; init; }
    public double ProductivityMultiplier { get; init; }
    public TimeSpan AverageTime { get; init; }
    public int FeatureCount { get; init; }
    public double SuccessRate { get; init; }
} 