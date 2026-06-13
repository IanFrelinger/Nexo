namespace Nexo.CLI.Commands;

internal sealed record WorkflowGatePolicy
{
    public string? Name { get; init; }
    public string? BenchmarkSet { get; init; }
    public double? MinSuccessRateDelta { get; init; }
    public long? MaxP95LatencyRegressionMs { get; init; }
    public long? MaxAverageLatencyRegressionMs { get; init; }
    public double? MinAverageScoreDelta { get; init; }
    public int? MaxRegressedScenarios { get; init; }
}

internal sealed record GatePolicyLoadResult(
    bool Ok,
    WorkflowGatePolicy? Policy,
    string? Error);
