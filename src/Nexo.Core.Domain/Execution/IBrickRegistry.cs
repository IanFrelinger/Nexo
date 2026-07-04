using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution.Events;

namespace Nexo.Core.Domain.Execution;

/// <summary>
/// Registry for looking up bricks by ID.
/// </summary>
public interface IBrickRegistry
{
    /// <summary>Looks up a brick by id; returns null when not registered.</summary>
    global::Nexo.Core.Domain.Bricks.Brick? GetBrick(string id);

    /// <summary>Returns all registered bricks.</summary>
    IReadOnlyList<global::Nexo.Core.Domain.Bricks.Brick> GetAllBricks();
}
