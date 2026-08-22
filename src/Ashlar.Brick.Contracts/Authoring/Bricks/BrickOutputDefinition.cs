namespace Ashlar.Core.Domain.Bricks;

/// <summary>
/// Definition of a brick output parameter.
/// </summary>
public class BrickOutputDefinition
{
    /// <summary>Output field name returned in execute response payloads.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Logical type name (e.g. <c>string</c>, <c>number</c>, <c>bytes</c>).</summary>
    public string Type { get; init; } = default!;

    /// <summary>Optional description shown in catalog and composer UIs.</summary>
    public string? Description { get; init; }

    /// <summary>Defines a brick output field.</summary>
    /// <param name="name">Output field name.</param>
    /// <param name="type">Logical type name.</param>
    /// <param name="description">Optional UI description.</param>
    public BrickOutputDefinition(string name, string type, string? description = null)
    {
        Name = name;
        Type = type;
        Description = description;
    }
}
