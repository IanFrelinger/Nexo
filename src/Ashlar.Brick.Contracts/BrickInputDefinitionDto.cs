namespace Ashlar.Brick.Contracts;
/// <summary>
/// Wire DTO for a brick input parameter definition.
/// </summary>
public class BrickInputDefinitionDto
{
    /// <summary>Parameter name referenced in execute input payloads.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Logical type name (e.g. <c>string</c>, <c>number</c>, <c>bytes</c>).</summary>
    public string Type { get; set; } = default!;

    /// <summary>Optional description shown in catalog and composer UIs.</summary>
    public string? Description { get; set; }

    /// <summary>Whether the caller must supply this input on execute.</summary>
    public bool Required { get; set; } = true;

    /// <summary>Default value when the caller omits this input and <see cref="Required"/> is false.</summary>
    public object? Default { get; set; }
}
