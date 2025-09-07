using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Productivity trends result
/// </summary>
public record ProductivityTrendsResult
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Granularity { get; init; } = string.Empty;
    public List<TrendDataPoint> DataPoints { get; init; } = new();
    public TrendAnalysis Analysis { get; init; } = new();
    public List<TrendForecast> Forecasts { get; init; } = new();
} 