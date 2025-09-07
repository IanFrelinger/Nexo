using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Represents a usage trend in the system.
/// </summary>
public record UsageTrend
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TrendType { get; init; } = string.Empty;
    public double TrendValue { get; init; }
    public double ChangeRate { get; init; }
    public string Direction { get; init; } = string.Empty; // "increasing", "decreasing", "stable"
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public List<DataPoint> DataPoints { get; init; } = new();
    public double Confidence { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Represents a data point in a trend.
/// </summary>
public record DataPoint
{
    public DateTime Timestamp { get; init; }
    public double Value { get; init; }
    public Dictionary<string, object> Attributes { get; init; } = new();
}
