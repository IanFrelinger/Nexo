using Nexo.Core.Domain.Behaviors;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// In-memory registry for behaviors. Implements Core.Domain.Execution.IBehaviorRegistry.
/// </summary>
public class BehaviorRegistry : Nexo.Core.Domain.Execution.IBehaviorRegistry
{
    private readonly Dictionary<string, Behavior> _behaviors = new();
    
    public BehaviorRegistry(IEnumerable<Behavior> behaviors)
    {
        foreach (var behavior in behaviors)
        {
            _behaviors[behavior.Id] = behavior;
        }
    }
    
    public Behavior? GetBehavior(string id)
    {
        return _behaviors.TryGetValue(id, out var behavior) ? behavior : null;
    }
    
    public IReadOnlyList<Behavior> GetAllBehaviors()
    {
        return _behaviors.Values.ToList();
    }
}

