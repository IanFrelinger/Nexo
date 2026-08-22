using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Behaviors;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution.Events;

namespace Ashlar.Core.Domain.Execution;

/// <summary>
/// Registry for looking up bricks by ID.
/// </summary>
public interface IBrickRegistry
{
    /// <summary>Looks up a brick by id; returns null when not registered.</summary>
    global::Ashlar.Core.Domain.Bricks.Brick? GetBrick(string id);

    /// <summary>Returns all registered bricks.</summary>
    IReadOnlyList<global::Ashlar.Core.Domain.Bricks.Brick> GetAllBricks();
}
