using Nexo.Core.Domain.Behaviors;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Registry for looking up behaviors by ID.
/// </summary>
public interface IBehaviorRegistry
{
    Behavior? GetBehavior(string id);
    IReadOnlyList<Behavior> GetAllBehaviors();
}

