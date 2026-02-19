using Nexo.Core.Domain.Bricks;

namespace Nexo.Agents.UniversalTester.Bricks;

/// <summary>
/// Default brick registry built from the standard Universal Tester bricks.
/// </summary>
public sealed class UniversalTesterBrickRegistry : IBrickRegistry
{
    private readonly Dictionary<string, Brick> _bricks;

    /// <summary>Creates a registry from a dictionary of brick ID to Brick instance.</summary>
    public UniversalTesterBrickRegistry(Dictionary<string, Brick> bricks)
    {
        _bricks = new Dictionary<string, Brick>(bricks, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets the brick by ID (case-insensitive); null if not found.</summary>
    /// <summary>Gets the brick by ID (case-insensitive); null if not found.</summary>
    public Brick? GetBrick(string id) =>
        _bricks.TryGetValue(id ?? "", out var b) ? b : null;

    /// <summary>Returns the list of registered brick IDs.</summary>
    /// <summary>Returns the list of registered brick IDs.</summary>
    public IReadOnlyList<string> GetRegisteredIds() => _bricks.Keys.ToList();
}
