using Nexo.Core.Application.Execution.Models;

namespace Nexo.Core.Application.Execution.Ports;

/// <summary>
/// Manages per-step execution mode (deterministic vs agentic).
/// Swap takes effect on next execution; in-flight executions complete with current mode.
/// </summary>
public interface IStepExecutionMode
{
    ExecutionMode GetMode(string stepId);
    Task SetModeAsync(string stepId, ExecutionMode mode, CancellationToken ct = default);
    Task<HotSwapResult> SwapAsync(string stepId, ExecutionMode targetMode, CancellationToken ct = default);
}
