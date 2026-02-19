using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// In-memory registry for bricks. Implements Core.Domain.Execution.IBrickRegistry.
/// </summary>
public class BrickRegistry : Nexo.Core.Domain.Execution.IBrickRegistry
{
    private readonly Dictionary<string, Brick> _bricks = new();
    
    /// <summary>
    /// Creates a new brick registry with the given bricks.
    /// </summary>
    /// <param name="bricks">Bricks to register.</param>
    public BrickRegistry(IEnumerable<Brick> bricks)
    {
        foreach (var brick in bricks)
        {
            _bricks[brick.Id] = brick;
        }
    }
    
    /// <inheritdoc />
    public Brick? GetBrick(string id)
    {
        return _bricks.TryGetValue(id, out var brick) ? brick : null;
    }
    
    /// <inheritdoc />
    public IReadOnlyList<Brick> GetAllBricks()
    {
        return _bricks.Values.ToList();
    }
}

