namespace Ashlar.CLI.Commands.Workflow;
internal sealed record WorkflowGatePolicy
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
