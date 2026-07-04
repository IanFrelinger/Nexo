using Nexo.Orchestration.Models;

namespace Nexo.CLI.Runtime;

/// <summary>Execution controls for workflow lab stress and benchmark runs.</summary>
public sealed record WorkflowLabExecutionSpec
{
    /// <summary>Number of measured iterations per scenario.</summary>
    public int Iterations { get; init; } = 1;

    /// <summary>When true, append run rows to workflow lab history.</summary>
    public bool PersistHistory { get; init; } = true;

    /// <summary>Benchmark set label stored with history rows.</summary>
    public string BenchmarkSet { get; init; } = "workflow-lab";

    /// <summary>When true, skip scenarios when the configured provider is unavailable.</summary>
    public bool SkipScenariosWhenProviderUnavailable { get; init; } = true;

    /// <summary>When true, randomize scenario execution order each run.</summary>
    public bool ShuffleScenarioOrder { get; init; }

    /// <summary>Optional random seed used when shuffling scenario order.</summary>
    public int? RandomSeed { get; init; }

    /// <summary>Discarded warmup iterations executed before measured runs.</summary>
    public int WarmupRuns { get; init; }

    /// <summary>Cooldown delay in milliseconds between measured iterations.</summary>
    public int CooldownMs { get; init; }
}
