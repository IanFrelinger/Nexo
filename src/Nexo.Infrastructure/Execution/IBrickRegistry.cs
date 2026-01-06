using Nexo.Core.Domain.Bricks;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Registry for looking up bricks by ID.
/// </summary>
public interface IBrickRegistry
{
    Brick? GetBrick(string id);
    IReadOnlyList<Brick> GetAllBricks();
}

