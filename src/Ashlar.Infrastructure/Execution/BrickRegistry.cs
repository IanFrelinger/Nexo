using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// In-memory registry for bricks. Implements Core.Domain.Execution.IBrickRegistry.
/// </summary>
public class BrickRegistry : Ashlar.Core.Domain.Execution.IBrickRegistry
{
    private readonly Dictionary<string, DomainBrick> _bricks = new();
    
    /// <summary>
    /// Creates a new brick registry with the given bricks.
    /// </summary>
    /// <param name="bricks">Bricks to register.</param>
    public BrickRegistry(IEnumerable<DomainBrick> bricks)
    {
        foreach (var brick in bricks)
        {
            _bricks[brick.Id] = brick;
        }
    }
    
    /// <inheritdoc />
    public DomainBrick? GetBrick(string id)
    {
        return _bricks.TryGetValue(id, out var brick) ? brick : null;
    }
    
    /// <inheritdoc />
    public IReadOnlyList<DomainBrick> GetAllBricks()
    {
        return _bricks.Values.ToList();
    }
}

