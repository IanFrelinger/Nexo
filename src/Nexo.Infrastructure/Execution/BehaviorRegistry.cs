using Nexo.Core.Domain.Behaviors;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// In-memory registry for behaviors. Implements Core.Domain.Execution.IBehaviorRegistry.
/// </summary>
public class BehaviorRegistry : Nexo.Core.Domain.Execution.IBehaviorRegistry
{
    private readonly Dictionary<string, Behavior> _behaviors = new();
    
    /// <summary>
    /// Creates a new behavior registry with the given behaviors.
    /// </summary>
    /// <param name="behaviors">Behaviors to register.</param>
    public BehaviorRegistry(IEnumerable<Behavior> behaviors)
    {
        foreach (var behavior in behaviors)
        {
            _behaviors[behavior.Id] = behavior;
        }
    }
    
    /// <inheritdoc />
    public Behavior? GetBehavior(string id)
    {
        return _behaviors.TryGetValue(id, out var behavior) ? behavior : null;
    }
    
    /// <inheritdoc />
    public IReadOnlyList<Behavior> GetAllBehaviors()
    {
        return _behaviors.Values.ToList();
    }
}

