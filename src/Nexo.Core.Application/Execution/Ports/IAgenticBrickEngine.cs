using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Execution.Ports;

/// <summary>
/// Bridge between agentic brick execution and NCR model selection.
/// </summary>
public interface IAgenticBrickEngine
{
    /// <summary>
    /// Resolves which model/provider should back this brick execution.
    /// </summary>
    Task<ModelResolution> ResolveModelForBrickAsync(
        Brick brick,
        IExecutionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Records execution outcome as a scoring feedback signal.
    /// </summary>
    Task RecordExecutionOutcomeAsync(
        ModelResolution resolution,
        BrickExecutionOutcome outcome,
        CancellationToken ct = default);
}

/// <summary>
/// Simplified outcome signal used by model scoring feedback.
/// </summary>
public sealed record BrickExecutionOutcome
{
    public bool Succeeded { get; init; }
    public bool TimedOut { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorCode { get; init; }
}
