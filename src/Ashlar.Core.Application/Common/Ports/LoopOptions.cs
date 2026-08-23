namespace Ashlar.Core.Application.Common.Ports;

/// <summary>Options controlling loop iteration bounds and parallelism.</summary>
public sealed record LoopOptions
{
    /// <summary>Diagnostic name used in logs and telemetry.</summary>
    public string? Name { get; init; }

    /// <summary>Maximum number of iterations before stopping.</summary>
    public int? MaxIterations { get; init; }

    /// <summary>Maximum elapsed time before stopping.</summary>
    public TimeSpan? TimeBudget { get; init; }

    /// <summary>When true, parallel decorators may fan out iteration.</summary>
    public bool EnableParallel { get; init; }

    /// <summary>Maximum parallel tasks when <see cref="EnableParallel"/> is true.</summary>
    public int? MaxDegreeOfParallelism { get; init; }
}
