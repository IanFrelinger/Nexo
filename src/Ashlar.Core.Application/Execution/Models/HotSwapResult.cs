namespace Ashlar.Core.Application.Execution.Models;

/// <summary>
/// Result of a hot-swap operation.
/// </summary>
/// <param name="StepId">Step identifier that was swapped.</param>
/// <param name="PreviousMode">Execution mode before the swap.</param>
/// <param name="NewMode">Execution mode after the swap.</param>
/// <param name="Success">Whether the swap completed successfully.</param>
/// <param name="FailureReason">Human-readable reason when <paramref name="Success"/> is false.</param>
/// <param name="SwappedAt">UTC timestamp when the swap was attempted.</param>
public record HotSwapResult(
    string StepId,
    ExecutionMode PreviousMode,
    ExecutionMode NewMode,
    bool Success,
    string? FailureReason,
    DateTimeOffset SwappedAt);
