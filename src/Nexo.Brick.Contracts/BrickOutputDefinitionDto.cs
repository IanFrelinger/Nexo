namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for a brick output parameter definition.
/// </summary>
public class BrickOutputDefinitionDto
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? Description { get; set; }
}
