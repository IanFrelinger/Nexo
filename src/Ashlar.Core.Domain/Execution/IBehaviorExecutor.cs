using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Behaviors;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution.Events;

namespace Ashlar.Core.Domain.Execution;

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
