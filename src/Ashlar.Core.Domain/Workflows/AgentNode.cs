using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Clusters;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Domain.Workflows;

/// <summary>
/// An agent node in the workflow.
/// </summary>
public record AgentNode : WorkflowNode
{
    /// <summary>
    /// Reference to the agent definition.
    /// </summary>
    public string AgentId { get; init; } = "";
    
    /// <summary>
    /// Implementation mode for this agent.
    /// </summary>
    public ImplementationMode Mode { get; init; } = ImplementationMode.Auto;
    
    /// <summary>
    /// Per-behavior implementation overrides.
    /// </summary>
    public IReadOnlyDictionary<string, ImplementationMode> BehaviorOverrides { get; init; } 
        = new Dictionary<string, ImplementationMode>();
    
    /// <summary>
    /// Per-brick implementation overrides (within behaviors).
    /// </summary>
    public IReadOnlyDictionary<string, ImplementationType> BrickOverrides { get; init; } 
        = new Dictionary<string, ImplementationType>();
    
    /// <summary>
    /// Parameter values for this agent instance.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } 
        = new Dictionary<string, object>();
}
