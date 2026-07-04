namespace Nexo.Core.Domain.Clusters;

/// <summary>
/// A parameter that can be configured per cluster instance.
/// </summary>
public class ClusterParameter
{
    /// <summary>Parameter key referenced in mappings and instance overrides.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Human-readable label for composer UIs.</summary>
    public string DisplayName { get; init; } = default!;

    /// <summary>Description shown to operators configuring the cluster.</summary>
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
