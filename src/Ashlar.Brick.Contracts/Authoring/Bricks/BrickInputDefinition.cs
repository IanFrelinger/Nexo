namespace Ashlar.Core.Domain.Bricks;

/// <summary>
/// Definition of a brick input parameter.
/// </summary>
public class BrickInputDefinition
{
    /// <summary>Parameter name referenced in execute input payloads.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Logical type name (e.g. <c>string</c>, <c>number</c>, <c>bytes</c>).</summary>
    public string Type { get; init; } = default!;

    /// <summary>Optional description shown in catalog and composer UIs.</summary>
    public string? Description { get; init; }

    /// <summary>Whether the caller must supply this input on execute.</summary>
    public bool Required { get; init; } = true;

    /// <summary>Default value when the caller omits this input and <see cref="Required"/> is false.</summary>
    public object? Default { get; init; }

    /// <summary>Defines a brick input parameter.</summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="type">Logical type name.</param>
    /// <param name="description">Optional UI description.</param>
    /// <param name="required">Whether the input is required.</param>
    /// <param name="defaultValue">Default value when not required.</param>
    public BrickInputDefinition(string name, string type, string? description = null, bool required = true, object? defaultValue = null)
    {
        Name = name;
        Type = type;
        Description = description;
        Required = required;
        Default = defaultValue;
    }
}
