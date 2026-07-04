using Nexo.Core.Application.Execution.Models;

namespace Nexo.Core.Application.Execution.Ports;

/// <summary>
/// Manages per-step execution mode (deterministic vs agentic).
/// Swap takes effect on next execution; in-flight executions complete with current mode.
/// </summary>
public interface IStepExecutionMode
{
    /// <summary>Returns the current execution mode for the given step.</summary>
    /// <param name="stepId">Step identifier within the workflow or pipeline.</param>
    ExecutionMode GetMode(string stepId);

    /// <summary>Persists a new execution mode for the given step.</summary>
    /// <param name="stepId">Step identifier to update.</param>
    /// <param name="mode">Target execution mode.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    Task SetModeAsync(string stepId, ExecutionMode mode, CancellationToken ct = default);

    /// <summary>
    /// Atomically swaps the execution mode, returning details of the transition.
    /// </summary>
    /// <param name="stepId">Step identifier to swap.</param>
    /// <param name="targetMode">Desired execution mode after the swap.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>Outcome of the swap including previous and new modes.</returns>
    Task<HotSwapResult> SwapAsync(string stepId, ExecutionMode targetMode, CancellationToken ct = default);
}
