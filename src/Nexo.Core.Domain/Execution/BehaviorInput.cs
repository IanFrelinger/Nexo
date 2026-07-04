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
