using System.Collections.Generic;

namespace Nexo.Feature.Input.Models;

/// <summary>
/// Input map definition
/// </summary>
public record InputMap
{
    /// <summary>
    /// Map name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Map description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Map priority
    /// </summary>
    public int Priority { get; init; } = 0;
    
    /// <summary>
    /// Actions in this map
    /// </summary>
    public List<string> Actions { get; init; } = new();
    
    /// <summary>
    /// Whether map is active
    /// </summary>
    public bool IsActive { get; init; } = true;
    
    /// <summary>
    /// Map conditions
    /// </summary>
    public List<InputCondition> Conditions { get; init; } = new();
}

/// <summary>
/// Input condition
/// </summary>
public record InputCondition
{
    /// <summary>
    /// Condition type
    /// </summary>
    public InputConditionType Type { get; init; }
    
    /// <summary>
    /// Condition value
    /// </summary>
    public object? Value { get; init; }
    
    /// <summary>
    /// Condition operator
    /// </summary>
    public InputConditionOperator Operator { get; init; }
}

/// <summary>
/// Input condition types
/// </summary>
public enum InputConditionType
{
    /// <summary>
    /// Game state condition
    /// </summary>
    GameState,
    
    /// <summary>
    /// Player state condition
    /// </summary>
    PlayerState,
    
    /// <summary>
    /// Input device condition
    /// </summary>
    InputDevice,
    
    /// <summary>
    /// Custom condition
    /// </summary>
    Custom
}

/// <summary>
/// Input condition operators
/// </summary>
public enum InputConditionOperator
{
    /// <summary>
    /// Equals
    /// </summary>
    Equals,
    
    /// <summary>
    /// Not equals
    /// </summary>
    NotEquals,
    
    /// <summary>
    /// Greater than
    /// </summary>
    GreaterThan,
    
    /// <summary>
    /// Less than
    /// </summary>
    LessThan,
    
    /// <summary>
    /// Contains
    /// </summary>
    Contains,
    
    /// <summary>
    /// Not contains
    /// </summary>
    NotContains
}
