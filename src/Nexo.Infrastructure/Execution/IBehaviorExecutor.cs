using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Executes behaviors and streams execution events.
/// </summary>
public interface IBehaviorExecutor
{
    Task<BehaviorResult> ExecuteAsync(
        AgentCard agent,
        Behavior behavior,
        BehaviorInput input,
        ExecutionOptions options,
        CancellationToken cancellationToken = default);
    
    IAsyncEnumerable<ExecutionEvent> ExecuteWithEventsAsync(
        AgentCard agent,
        Behavior behavior,
        BehaviorInput input,
        ExecutionOptions options,
        CancellationToken cancellationToken = default);
}

