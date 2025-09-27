using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Interfaces;

/// <summary>
/// Performance benchmark request
/// </summary>
public record PerformanceBenchmarkRequest
{
    public List<string> BenchmarkTypes { get; init; } = new();
    public int IterationCount { get; init; }
    public TimeSpan BenchmarkDuration { get; init; }
    public bool IncludeLoadTesting { get; init; }
    public bool IncludeStressTesting { get; init; }
    public Dictionary<string, object> BenchmarkParameters { get; init; } = new();
}

/// <summary>
/// Performance benchmark result
/// </summary>
public record PerformanceBenchmarkResult
{
    public List<BenchmarkResult> Results { get; init; } = new();
    public PerformanceMetrics OverallMetrics { get; init; } = new();
    public LoadTestResult LoadTest { get; init; } = new();
    public StressTestResult StressTest { get; init; } = new();
    public List<PerformanceRecommendation> Recommendations { get; init; } = new();
    public DateTime CompletedAt { get; init; }
}

/// <summary>
/// Benchmark result
/// </summary>
public record BenchmarkResult
{
    public string BenchmarkType { get; init; } = string.Empty;
    public int IterationCount { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public TimeSpan AverageDuration { get; init; }
    public TimeSpan MinDuration { get; init; }
    public TimeSpan MaxDuration { get; init; }
    public double Throughput { get; init; }
    public double SuccessRate { get; init; }
    public Dictionary<string, double> DetailedMetrics { get; init; } = new();
}

/// <summary>
/// Performance metrics
/// </summary>
public record PerformanceMetrics
{
    public TimeSpan AverageResponseTime { get; init; }
    public double RequestsPerSecond { get; init; }
    public double ErrorRate { get; init; }
    public double ResourceUtilization { get; init; }
    public double ScalabilityScore { get; init; }
}

/// <summary>
/// Load test result
/// </summary>
public record LoadTestResult
{
    public int ConcurrentUsers { get; init; }
    public TimeSpan TestDuration { get; init; }
    public double AverageResponseTime { get; init; }
    public double Throughput { get; init; }
    public double ErrorRate { get; init; }
    public List<LoadTestDataPoint> DataPoints { get; init; } = new();
}

/// <summary>
/// Load test data point
/// </summary>
public record LoadTestDataPoint
{
    public DateTime Timestamp { get; init; }
    public int ConcurrentUsers { get; init; }
    public double ResponseTime { get; init; }
    public double Throughput { get; init; }
    public double ErrorRate { get; init; }
}

/// <summary>
/// Stress test result
/// </summary>
public record StressTestResult
{
    public int MaxConcurrentUsers { get; init; }
    public TimeSpan TimeToFailure { get; init; }
    public string FailureMode { get; init; } = string.Empty;
    public double RecoveryTime { get; init; }
    public List<string> Bottlenecks { get; init; } = new();
}

/// <summary>
/// Performance recommendation
/// </summary>
public record PerformanceRecommendation
{
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
    public string Effort { get; init; } = string.Empty;
    public double ExpectedImprovement { get; init; }
}
