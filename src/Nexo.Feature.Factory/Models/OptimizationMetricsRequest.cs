using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization metrics request
/// </summary>
public record OptimizationMetricsRequest
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? ProjectId { get; init; }
    public List<string> MetricTypes { get; init; } = new();
    public bool IncludeTrends { get; init; }
    public Dictionary<string, object> MetricsParameters { get; init; } = new();
} 