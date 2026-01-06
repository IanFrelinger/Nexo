using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// In-memory registry for bricks.
/// </summary>
public class BrickRegistry : IBrickRegistry
{
    private readonly Dictionary<string, Brick> _bricks = new();
    
    public BrickRegistry(IEnumerable<Brick> bricks)
    {
        foreach (var brick in bricks)
        {
            _bricks[brick.Id] = brick;
        }
    }
    
    public Brick? GetBrick(string id)
    {
        return _bricks.TryGetValue(id, out var brick) ? brick : null;
    }
    
    public IReadOnlyList<Brick> GetAllBricks()
    {
        return _bricks.Values.ToList();
    }
}

