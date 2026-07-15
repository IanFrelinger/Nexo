using Nexo.Core.Domain.Bricks;
using Nexo.GameDomain.Bricks;

namespace Nexo.GameDomain;

/// <summary>
/// In-memory brick registry for gameplay hosts. Does not reference Infrastructure.
/// </summary>
public sealed class GameplayBrickRegistry
{
    private readonly Dictionary<string, DomainBrick> _bricks;

    /// <summary>Creates a registry seeded with the given bricks.</summary>
    public GameplayBrickRegistry(IEnumerable<DomainBrick> bricks)
    {
        _bricks = new Dictionary<string, DomainBrick>(StringComparer.OrdinalIgnoreCase);
        foreach (var brick in bricks ?? throw new ArgumentNullException(nameof(bricks)))
        {
            if (string.IsNullOrWhiteSpace(brick.Id))
                throw new ArgumentException("Brick Id is required.", nameof(bricks));
            _bricks[brick.Id] = brick;
        }
    }

    /// <summary>Looks up a brick by id; returns null when missing.</summary>
    public DomainBrick? GetBrick(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return _bricks.TryGetValue(id, out var brick) ? brick : null;
    }

    /// <summary>Returns all registered bricks.</summary>
    public IReadOnlyList<DomainBrick> GetAllBricks() => _bricks.Values.ToList();

    /// <summary>Registers or replaces a brick.</summary>
    public void Register(DomainBrick brick)
    {
        if (brick is null)
            throw new ArgumentNullException(nameof(brick));
        if (string.IsNullOrWhiteSpace(brick.Id))
            throw new ArgumentException("Brick Id is required.", nameof(brick));
        _bricks[brick.Id] = brick;
    }

    /// <summary>Default set: damage resolver, ability gate, score aggregate.</summary>
    public static GameplayBrickRegistry CreateDefault()
        => new(GameplayBricks.CreateDefaults());
}
