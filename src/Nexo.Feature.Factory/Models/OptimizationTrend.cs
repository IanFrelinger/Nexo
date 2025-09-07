using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization trend
/// </summary>
public record OptimizationTrend
{
    public string TrendId { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public double TrendStrength { get; init; }
    public List<TrendDataPoint> DataPoints { get; init; } = new();
    public DateTime TrendStart { get; init; }
    public DateTime TrendEnd { get; init; }
    public Dictionary<string, object> TrendData { get; init; } = new();
} 