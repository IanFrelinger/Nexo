namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// Interface contract defining inputs and outputs for a brick.
/// </summary>
public class BrickInterface
{
    /// <summary>
    /// Input parameters this brick accepts.
    /// </summary>
    public IReadOnlyList<BrickInputDefinition> Inputs { get; init; } = [];
    
    /// <summary>
    /// Output parameters this brick produces.
    /// </summary>
    public IReadOnlyList<BrickOutputDefinition> Outputs { get; init; } = [];
}

/// <summary>
/// Definition of a brick input parameter.
/// </summary>
public class BrickInputDefinition
{
    public string Name { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string? Description { get; init; }
    public bool Required { get; init; } = true;
    public object? Default { get; init; }
    
    public BrickInputDefinition(string name, string type, string? description = null, bool required = true, object? defaultValue = null)
    {
        Name = name;
        Type = type;
        Description = description;
        Required = required;
        Default = defaultValue;
    }
}

/// <summary>
/// Definition of a brick output parameter.
/// </summary>
public class BrickOutputDefinition
{
    public string Name { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string? Description { get; init; }
    
    public BrickOutputDefinition(string name, string type, string? description = null)
    {
        Name = name;
        Type = type;
        Description = description;
    }
}
