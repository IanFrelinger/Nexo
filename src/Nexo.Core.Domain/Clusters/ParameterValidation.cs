namespace Nexo.Core.Domain.Clusters;

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
