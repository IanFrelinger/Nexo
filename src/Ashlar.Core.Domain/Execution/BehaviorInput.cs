using Ashlar.Core.Domain.Agents;
using Ashlar.Core.Domain.Behaviors;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution.Events;

namespace Ashlar.Core.Domain.Execution;

/// <summary>
/// Input for executing a behavior.
/// </summary>
public class BehaviorInput
{
    /// <summary>Named parameter values supplied to the behavior.</summary>
    public Dictionary<string, object> Parameters { get; init; } = new();

    /// <summary>Creates an empty behavior input.</summary>
    public BehaviorInput()
    {
    }

    /// <summary>Creates a behavior input from an existing parameter map.</summary>
    /// <param name="parameters">Initial parameter values.</param>
    public BehaviorInput(Dictionary<string, object> parameters)
    {
        Parameters = parameters;
    }
}
