namespace Nexo.Brick.Contracts;
/// <summary>
/// Wire DTO for a brick's interface (inputs and outputs).
/// </summary>
public class BrickInterfaceDto
{
    /// <summary>Declared input parameters accepted by execute requests.</summary>
    public IReadOnlyList<BrickInputDefinitionDto> Inputs { get; set; } = [];

    /// <summary>Declared output fields returned by execute responses.</summary>
    public IReadOnlyList<BrickOutputDefinitionDto> Outputs { get; set; } = [];
}
