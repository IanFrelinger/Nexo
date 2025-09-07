using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Usage anomaly
/// </summary>
public record UsageAnomaly
{
    public string AnomalyId { get; init; } = string.Empty;
    public string AnomalyType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Severity { get; init; }
    public DateTime DetectedAt { get; init; }
    public List<string> AffectedComponents { get; init; } = new();
    public Dictionary<string, object> AnomalyData { get; init; } = new();
} 