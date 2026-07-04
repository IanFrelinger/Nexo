using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution.Events;

namespace Nexo.Core.Domain.Execution;

/// <summary>
/// Registry for looking up behaviors by ID.
/// </summary>
public interface IBehaviorRegistry
{
    /// <summary>Looks up a behavior by id; returns null when not registered.</summary>
    Behavior? GetBehavior(string id);

    /// <summary>Returns all registered behaviors.</summary>
    IReadOnlyList<Behavior> GetAllBehaviors();
}
