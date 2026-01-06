namespace Nexo.Infrastructure.Execution;

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

