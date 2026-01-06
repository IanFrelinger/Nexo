namespace Nexo.Core.Domain.Clusters;

/// <summary>
/// A parameter that can be configured per cluster instance.
/// </summary>
public class ClusterParameter
{
    public string Name { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string Description { get; init; } = default!;
    
    /// <summary>
    /// Type of the parameter (string, int, float, bool, enum, object).
    /// </summary>
    public string Type { get; init; } = "string";
    
    /// <summary>
    /// Default value.
    /// </summary>
    public object? Default { get; init; }
    
    /// <summary>
    /// For numeric types: minimum value.
    /// </summary>
    public object? Min { get; init; }
    
    /// <summary>
    /// For numeric types: maximum value.
    /// </summary>
    public object? Max { get; init; }
    
    /// <summary>
    /// For enum types: allowed values.
    /// </summary>
    public IReadOnlyList<string>? AllowedValues { get; init; }
    
    /// <summary>
    /// Validation rules.
    /// </summary>
    public IReadOnlyList<ParameterValidation>? Validations { get; init; }
    
    /// <summary>
    /// UI hints for rendering this parameter.
    /// </summary>
    public ParameterUIHints? UIHints { get; init; }
}

/// <summary>
/// Validation rule for a parameter.
/// </summary>
public class ParameterValidation
{
    public string Type { get; init; } = default!;
    public string Value { get; init; } = default!;
    public string Message { get; init; } = default!;
    
    public ParameterValidation(string type, string value, string message)
    {
        Type = type;
        Value = value;
        Message = message;
    }
}

/// <summary>
/// UI hints for parameter rendering.
/// </summary>
public class ParameterUIHints
{
    public string? ControlType { get; init; } // "slider", "dropdown", "text", "color", etc.
    public string? Group { get; init; }
    public int? Order { get; init; }
    public bool Advanced { get; init; } = false;
}

