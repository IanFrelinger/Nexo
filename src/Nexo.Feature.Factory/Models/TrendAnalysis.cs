using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Trend analysis
/// </summary>
public record TrendAnalysis
{
    public string OverallTrend { get; init; } = string.Empty;
    public double TrendStrength { get; init; }
    public List<string> KeyInsights { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
} 