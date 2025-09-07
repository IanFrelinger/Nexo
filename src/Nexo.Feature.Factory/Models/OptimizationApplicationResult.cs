using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization application result
/// </summary>
public record OptimizationApplicationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<AppliedOptimization> AppliedOptimizations { get; init; } = new();
    public int OptimizationsApplied { get; init; }
    public int OptimizationsFailed { get; init; }
    public double ApplicationSuccessRate { get; init; }
    public TimeSpan ApplicationDuration { get; init; }
    public Dictionary<string, double> ApplicationMetrics { get; init; } = new();
    public DateTime AppliedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 