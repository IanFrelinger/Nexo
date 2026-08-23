namespace Ashlar.Brick.Contracts;
/// <summary>
/// Wire DTO for a brick output parameter definition.
/// </summary>
public class BrickOutputDefinitionDto
{
    /// <summary>Output field name returned in execute response payloads.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Logical type name (e.g. <c>string</c>, <c>number</c>, <c>bytes</c>).</summary>
    public string Type { get; set; } = default!;

    /// <summary>Optional description shown in catalog and composer UIs.</summary>
    public string? Description { get; set; }
}
