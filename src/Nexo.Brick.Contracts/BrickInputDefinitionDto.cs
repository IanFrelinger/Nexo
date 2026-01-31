namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for a brick input parameter definition.
/// </summary>
public class BrickInputDefinitionDto
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? Description { get; set; }
    public bool Required { get; set; } = true;
    public object? Default { get; set; }
}
