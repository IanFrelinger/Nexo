using System;
using System.Collections.Generic;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Agents.Specialized;

/// <summary>
/// Performance analysis result
/// </summary>
public record PerformanceAnalysis
{
    public bool RequiresOptimization { get; init; }
    public PerformanceComplexity ComplexityLevel { get; init; }
    public string[] Recommendations { get; init; } = [];
}

/// <summary>
/// Performance complexity levels
/// </summary>
public enum PerformanceComplexity
{
    Low,
    Medium,
    High
}

/// <summary>
/// Iteration optimization result
/// </summary>
public record IterationOptimization
{
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double ExpectedImprovement { get; init; } = 1.0;
}

/// <summary>
/// Performance gains from optimization
/// </summary>
public record PerformanceGains
{
    public double ImprovementFactor { get; init; } = 1.0;
    public Dictionary<string, object>? BenchmarkResults { get; init; }
}

/// <summary>
/// Platform-specific optimization result
/// </summary>
public record PlatformOptimization
{
    public PlatformCompatibility Platform { get; init; }
    public string OptimizedCode { get; init; } = string.Empty;
    public PerformanceGains? PerformanceGains { get; init; }
}
