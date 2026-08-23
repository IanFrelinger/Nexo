using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Ports;

/// <summary>
/// Simplified outcome signal used by model scoring feedback.
/// </summary>
public sealed record BrickExecutionOutcome
{
    /// <summary>Whether the brick execution completed without error.</summary>
    public bool Succeeded { get; init; }

    /// <summary>Whether the execution exceeded its allotted time limit.</summary>
    public bool TimedOut { get; init; }

    /// <summary>Wall-clock time spent executing the brick.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Machine-readable error code when <see cref="Succeeded"/> is false.</summary>
    public string? ErrorCode { get; init; }
}
