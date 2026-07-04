using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution.Events;

namespace Nexo.Core.Domain.Execution;

/// <summary>
/// Executes behaviors and streams execution events.
/// </summary>
public interface IBehaviorExecutor
{
    /// <summary>Executes a behavior and returns the final result.</summary>
    Task<BehaviorResult> ExecuteAsync(
        AgentCard agent,
        Behavior behavior,
        BehaviorInput input,
        ExecutionOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>Executes a behavior and streams lifecycle <see cref="ExecutionEvent"/> instances.</summary>
    IAsyncEnumerable<ExecutionEvent> ExecuteWithEventsAsync(
        AgentCard agent,
        Behavior behavior,
        BehaviorInput input,
        ExecutionOptions options,
        CancellationToken cancellationToken = default);
}
