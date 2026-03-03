namespace Nexo.Core.Application.Execution.Models;

/// <summary>
/// Result of a hot-swap operation.
/// </summary>
public record HotSwapResult(
    string StepId,
    ExecutionMode PreviousMode,
    ExecutionMode NewMode,
    bool Success,
    string? FailureReason,
    DateTimeOffset SwappedAt);
