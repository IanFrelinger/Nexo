using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Behaviors;

/// <summary>
/// A Behavior is a named pipeline of brick steps that accomplishes a goal.
/// </summary>
public class Behavior
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Version { get; init; } = "1.0.0";
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

/// <summary>
/// A single step in a behavior pipeline.
/// </summary>
public class BehaviorStep
{
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

/// <summary>
/// Policy for handling step failures.
/// </summary>
public enum FailurePolicy
{
    Abort,
    Continue,
    Retry,
    Fallback
}

/// <summary>
/// Criteria for determining behavior success.
/// </summary>
public class SuccessCriteria
{
    public bool AllStepsComplete { get; init; } = true;
    public IReadOnlyList<ValidationRule>? OutputValidation { get; init; }
    public string? CustomExpression { get; init; }
}

/// <summary>
/// Input or output parameter for a behavior.
/// </summary>
public class BehaviorParameter
{
    public string Name { get; init; } = default!;
    public string Type { get; init; } = default!;
    public bool Required { get; init; } = true;
    public object? Default { get; init; }
    public string? Description { get; init; }
    
    public BehaviorParameter(string name, string type, bool required = true, object? defaultValue = null, string? description = null)
    {
        Name = name;
        Type = type;
        Required = required;
        Default = defaultValue;
        Description = description;
    }
}

/// <summary>
/// Validation rule for behavior outputs.
/// </summary>
public class ValidationRule
{
    public string Field { get; init; } = default!;
    public string Rule { get; init; } = default!;
    public object? Value { get; init; }
    
    public ValidationRule(string field, string rule, object? value = null)
    {
        Field = field;
        Rule = rule;
        Value = value;
    }
}

