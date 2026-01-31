namespace Nexo.BrickContracts;

/// <summary>
/// Wire DTO for a brick's interface (inputs and outputs).
/// </summary>
public class BrickInterfaceDto
{
    public IReadOnlyList<BrickInputDefinitionDto> Inputs { get; set; } = [];
    public IReadOnlyList<BrickOutputDefinitionDto> Outputs { get; set; } = [];
}
