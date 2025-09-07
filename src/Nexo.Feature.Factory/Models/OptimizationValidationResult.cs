using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Models;

/// <summary>
/// Optimization validation result
/// </summary>
public record OptimizationValidationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<ValidatedOptimization> ValidatedOptimizations { get; init; } = new();
    public int OptimizationsValidated { get; init; }
    public int ValidOptimizations { get; init; }
    public int InvalidOptimizations { get; init; }
    public double ValidationSuccessRate { get; init; }
    public TimeSpan ValidationDuration { get; init; }
    public Dictionary<string, double> ValidationMetrics { get; init; } = new();
    public DateTime ValidatedAt { get; init; }
    public string? ErrorMessage { get; init; }
} 