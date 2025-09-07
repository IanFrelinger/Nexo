using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Industry trend
/// </summary>
public record IndustryTrend
{
    public string TrendId { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public string TrendType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double TrendStrength { get; init; }
    public string Direction { get; init; } = string.Empty;
    public List<TrendDataPoint> DataPoints { get; init; } = new();
    public DateTime TrendStart { get; init; }
    public DateTime TrendEnd { get; init; }
    public Dictionary<string, object> TrendData { get; init; } = new();
} 