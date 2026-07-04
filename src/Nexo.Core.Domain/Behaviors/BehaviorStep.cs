using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Behaviors;

/// <summary>
/// A single step in a behavior pipeline.
/// </summary>
public class BehaviorStep
{
    /// <summary>Stable step identifier within the parent behavior.</summary>
    public string Id { get; init; } = default!;
    
    /// <summary>
    /// Reference to the brick to execute.
    /// </summary>
    public string BrickId { get; init; } = default!;
    
    /// <summary>
    /// Preferred implementation for this step.
    /// </summary>
    public ImplementationType Implementation { get; init; } = ImplementationType.Auto;
    
    /// <summary>
    /// Map behavior inputs/context to brick inputs.
    /// Key = brick input name, Value = expression to evaluate.
    /// </summary>
    public IReadOnlyDictionary<string, string> InputMapping { get; init; } = 
        new Dictionary<string, string>();
    
    /// <summary>
    /// Map brick outputs to behavior context.
    /// Key = context variable name, Value = brick output expression.
    /// </summary>
    public IReadOnlyDictionary<string, string> OutputMapping { get; init; } = 
        new Dictionary<string, string>();
    
    /// <summary>
    /// Optional condition for running this step.
    /// </summary>
    public string? Condition { get; init; }
    
    /// <summary>
    /// Step-specific configuration overrides.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Config { get; init; }
}
