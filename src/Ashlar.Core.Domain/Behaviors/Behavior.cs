using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Behaviors;

/// <summary>
/// A Behavior is a named pipeline of brick steps that accomplishes a goal.
/// </summary>
public class Behavior
{
    /// <summary>Stable behavior identifier.</summary>
    public string Id { get; init; } = default!;

    /// <summary>Human-readable behavior name.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Semantic version of the behavior definition.</summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>Short description of the behavior goal.</summary>
    public string Description { get; init; } = default!;
    
    /// <summary>
    /// Ordered list of steps in this behavior.
    /// </summary>
    public IReadOnlyList<BehaviorStep> Steps { get; init; } = [];
    
    /// <summary>
    /// What to do when a step fails.
    /// </summary>
    public FailurePolicy OnStepFailure { get; init; } = FailurePolicy.Abort;
    
    /// <summary>
    /// Maximum retries per step.
    /// </summary>
    public int MaxRetries { get; init; } = 2;
    
    /// <summary>
    /// Overall timeout for the behavior.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Criteria for considering this behavior successful.
    /// </summary>
    public SuccessCriteria SuccessCriteria { get; init; } = new();
    
    /// <summary>
    /// Input parameters this behavior accepts.
    /// </summary>
    public IReadOnlyList<BehaviorParameter> Inputs { get; init; } = [];
    
    /// <summary>
    /// Output parameters this behavior produces.
    /// </summary>
    public IReadOnlyList<BehaviorParameter> Outputs { get; init; } = [];
}
