using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Behaviors;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution.Events;

namespace Ashlar.Core.Domain.Execution;

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
