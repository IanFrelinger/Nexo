using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Execution.Routing;

/// <summary>
/// Snapshot of a RunPod job's current state.
/// <see cref="IsTerminal"/> is true for <see cref="RunPodJobState.Completed"/>
/// and <see cref="RunPodJobState.Failed"/> — callers should stop polling once
/// it returns true.
/// </summary>
public sealed record JobStatus
{
    /// <summary>Current lifecycle state of the job.</summary>
    public RunPodJobState State { get; init; } = RunPodJobState.Unknown;

    /// <summary>Optional status message from the RunPod API.</summary>
    public string? Message { get; init; }

    /// <summary>Whether polling should stop because the job reached a terminal state.</summary>
    public bool IsTerminal =>
        State == RunPodJobState.Completed ||
        State == RunPodJobState.Failed;
}
