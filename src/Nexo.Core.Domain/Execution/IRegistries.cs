using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution.Events;

namespace Nexo.Core.Domain.Execution;

/// <summary>
/// Input for executing a behavior.
/// </summary>
public class BehaviorInput
{
    public Dictionary<string, object> Parameters { get; init; } = new();
    
    public BehaviorInput()
    {
    }
    
    public BehaviorInput(Dictionary<string, object> parameters)
    {
        Parameters = parameters;
    }
}

/// <summary>
/// Registry for looking up agent cards by ID.
/// </summary>
public interface IAgentRegistry
{
    AgentCard? GetAgent(string id);
    IReadOnlyList<AgentCard> GetAllAgents();
}

/// <summary>
/// Registry for looking up bricks by ID.
/// </summary>
public interface IBrickRegistry
{
    Brick? GetBrick(string id);
    IReadOnlyList<Brick> GetAllBricks();
}

/// <summary>
/// Registry for looking up behaviors by ID.
/// </summary>
public interface IBehaviorRegistry
{
    Behavior? GetBehavior(string id);
    IReadOnlyList<Behavior> GetAllBehaviors();
}

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
