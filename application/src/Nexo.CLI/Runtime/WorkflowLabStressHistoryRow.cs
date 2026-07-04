using System.Text.Json;

namespace Nexo.CLI.Runtime;

/// <summary>Append-only JSONL history row for a workflow lab stress run.</summary>
public sealed record WorkflowLabStressHistoryRow
{
    /// <summary>Run id.</summary>
    public string RunId { get; init; } = string.Empty;
    /// <summary>Git sha.</summary>
    public string GitSha { get; init; } = string.Empty;
    /// <summary>Spec hash.</summary>
    public string SpecHash { get; init; } = string.Empty;
    /// <summary>Provider snapshot.</summary>
    public string ProviderSnapshot { get; init; } = string.Empty;
    /// <summary>Scenario id.</summary>
    public string ScenarioId { get; init; } = string.Empty;
    /// <summary>Request id.</summary>
    public string RequestId { get; init; } = string.Empty;
    /// <summary>Composition id.</summary>
    public string CompositionId { get; init; } = string.Empty;
    /// <summary>Model profile id.</summary>
    public string ModelProfileId { get; init; } = string.Empty;
    /// <summary>Iteration.</summary>
    public int Iteration { get; init; }
    /// <summary>Started at utc.</summary>
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    /// <summary>Elapsed ms.</summary>
    public long ElapsedMs { get; init; }
    /// <summary>Success.</summary>
    public bool Success { get; init; }
    /// <summary>Agent count.</summary>
    public int AgentCount { get; init; }
    /// <summary>Conflict count.</summary>
    public int ConflictCount { get; init; }
    /// <summary>Escalation count.</summary>
    public int EscalationCount { get; init; }
    /// <summary>Score.</summary>
    public double Score { get; init; }
    /// <summary>Summary.</summary>
    public string? Summary { get; init; }
    /// <summary>Skipped.</summary>
    public bool Skipped { get; init; }
    /// <summary>Warmup.</summary>
    public bool Warmup { get; init; }
    /// <summary>Cpu time delta ms.</summary>
    public long CpuTimeDeltaMs { get; init; }
    /// <summary>Working set mb.</summary>
    public long WorkingSetMb { get; init; }
    /// <summary>Private memory mb.</summary>
    public long PrivateMemoryMb { get; init; }
    /// <summary>Managed memory mb.</summary>
    public long ManagedMemoryMb { get; init; }
    /// <summary>Thread count.</summary>
    public int ThreadCount { get; init; }
    /// <summary>Hardware profile.</summary>
    public string HardwareProfile { get; init; } = string.Empty;
    /// <summary>Failure category.</summary>
    public string FailureCategory { get; init; } = "none";
    /// <summary>Benchmark set.</summary>
    public string BenchmarkSet { get; init; } = "workflow-lab";
}
