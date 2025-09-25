namespace Nexo.Feature.Input.Models;

/// <summary>
/// Input action definition
/// </summary>
public record InputAction
{
    /// <summary>
    /// Action name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Action description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Action type
    /// </summary>
    public InputActionType Type { get; init; }
    
    /// <summary>
    /// Action value type
    /// </summary>
    public InputValueType ValueType { get; init; }
    
    /// <summary>
    /// Whether action is required
    /// </summary>
    public bool Required { get; init; } = true;
    
    /// <summary>
    /// Default value
    /// </summary>
    public object? DefaultValue { get; init; }
}

/// <summary>
/// Input action types
/// </summary>
public enum InputActionType
{
    /// <summary>
    /// Button press
    /// </summary>
    Button,
    
    /// <summary>
    /// Axis movement
    /// </summary>
    Axis,
    
    /// <summary>
    /// Vector2 movement
    /// </summary>
    Vector2,
    
    /// <summary>
    /// Vector3 movement
    /// </summary>
    Vector3,
    
    /// <summary>
    /// Quaternion rotation
    /// </summary>
    Quaternion,
    
    /// <summary>
    /// Custom action
    /// </summary>
    Custom
}

/// <summary>
/// Input value types
/// </summary>
public enum InputValueType
{
    /// <summary>
    /// Boolean value
    /// </summary>
    Boolean,
    
    /// <summary>
    /// Float value
    /// </summary>
    Float,
    
    /// <summary>
    /// Integer value
    /// </summary>
    Integer,
    
    /// <summary>
    /// Vector2 value
    /// </summary>
    Vector2,
    
    /// <summary>
    /// Vector3 value
    /// </summary>
    Vector3,
    
    /// <summary>
    /// Quaternion value
    /// </summary>
    Quaternion,
    
    /// <summary>
    /// String value
    /// </summary>
    String
}
