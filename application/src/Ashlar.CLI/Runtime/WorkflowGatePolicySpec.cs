using System.Text.Json;

namespace Ashlar.CLI.Runtime;

/// <summary>Gate policy thresholds captured with a promoted workflow baseline.</summary>
public sealed record WorkflowGatePolicySpec
{
    /// <summary>Name.</summary>
    public string? Name { get; init; }
    /// <summary>Benchmark set.</summary>
    public string? BenchmarkSet { get; init; }
    /// <summary>Min success rate delta.</summary>
    public double? MinSuccessRateDelta { get; init; }
    /// <summary>Max p95 latency regression ms.</summary>
    public long? MaxP95LatencyRegressionMs { get; init; }
    /// <summary>Max average latency regression ms.</summary>
    public long? MaxAverageLatencyRegressionMs { get; init; }
    /// <summary>Min average score delta.</summary>
    public double? MinAverageScoreDelta { get; init; }
    /// <summary>Max regressed scenarios.</summary>
    public int? MaxRegressedScenarios { get; init; }
}
