using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.CLI.Commands;

/// <summary>
/// Interface for iteration benchmarking
/// </summary>
public interface IIterationBenchmarker
{
    Task<IEnumerable<BenchmarkResult>> BenchmarkAllStrategies(int dataSize, string platform, int iterations);
}

/// <summary>
/// Interface for iteration code generation
/// </summary>
public interface IIterationCodeGenerator
{
    Task<string> GenerateOptimalIterationAsync(IterationCodeRequest request);
}

/// <summary>
/// Interface for iteration code optimization
/// </summary>
public interface IIterationCodeOptimizer
{
    Task<IterationOptimizationResult> OptimizeIterationCodeAsync(IterationOptimizationRequest request);
}

/// <summary>
/// Request for iteration code generation
/// </summary>
public record IterationCodeRequest
{
    public string Description { get; init; } = string.Empty;
    public PlatformTarget TargetPlatform { get; init; } = PlatformTarget.DotNet;
    public int EstimatedDataSize { get; init; } = 1000;
}

/// <summary>
/// Result from iteration optimization
/// </summary>
public record IterationOptimizationResult
{
    public string OptimizedCode { get; init; } = string.Empty;
    public OptimizationMetrics OptimizationMetrics { get; init; } = new();
    public IIterationStrategy<object>? SelectedStrategy { get; init; }
}

/// <summary>
/// Benchmark result
/// </summary>
public record BenchmarkResult
{
    public string StrategyId { get; init; } = string.Empty;
    public double ExecutionTime { get; init; }
    public double MemoryUsageMB { get; init; }
    public double PerformanceScore { get; init; }
    public string Platform { get; init; } = string.Empty;
    public bool IsRecommended { get; init; }
}
