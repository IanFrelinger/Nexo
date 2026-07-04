using System.Text.Json;

namespace Nexo.CLI.Runtime;

/// <summary>Promoted workflow lab baseline metadata persisted for gate comparisons.</summary>
public sealed record WorkflowBaselineRecord
{
    /// <summary>Baseline id.</summary>
    public string BaselineId { get; init; } = string.Empty;
    /// <summary>Benchmark set.</summary>
    public string BenchmarkSet { get; init; } = "workflow-lab";
    /// <summary>Run id.</summary>
    public string RunId { get; init; } = string.Empty;
    /// <summary>Git sha.</summary>
    public string GitSha { get; init; } = string.Empty;
    /// <summary>Spec hash.</summary>
    public string SpecHash { get; init; } = string.Empty;
    /// <summary>Provider snapshot.</summary>
    public string ProviderSnapshot { get; init; } = string.Empty;
    /// <summary>Promoted at utc.</summary>
    public DateTimeOffset PromotedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Active.</summary>
    public bool Active { get; init; } = true;
    /// <summary>Notes.</summary>
    public string? Notes { get; init; }
    /// <summary>Policy.</summary>
    public WorkflowGatePolicySpec? Policy { get; init; }
}
