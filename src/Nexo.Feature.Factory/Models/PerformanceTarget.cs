using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Performance target
/// </summary>
public record PerformanceTarget
{
    public string TargetId { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public double TargetValue { get; init; }
    public string Unit { get; init; } = string.Empty;
    public DateTime TargetDate { get; init; }
    public string Priority { get; init; } = string.Empty;
    public Dictionary<string, object> TargetData { get; init; } = new();
} 