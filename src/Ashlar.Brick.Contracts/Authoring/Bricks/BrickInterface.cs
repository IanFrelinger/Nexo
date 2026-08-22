namespace Ashlar.Core.Domain.Bricks;

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
