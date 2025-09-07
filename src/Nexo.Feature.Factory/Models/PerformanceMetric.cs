using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Performance metric
/// </summary>
public record PerformanceMetric
{
    public string MetricId { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public string MetricType { get; init; } = string.Empty;
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
    public DateTime MeasuredAt { get; init; }
    public Dictionary<string, object> MetricData { get; init; } = new();
} 