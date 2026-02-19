using Nexo.Core.Domain.Bricks;

namespace Nexo.Agents.UniversalTester.Bricks;

/// <summary>
/// Registry of bricks available for the Universal Tester pipeline.
/// Maps brick IDs (perception, understanding, exploration, action, validation, reporting) to Brick instances.
/// Enables optional bricks (e.g. world-model-prediction) when added.
/// </summary>
public interface IBrickRegistry
{
    /// <summary>
    /// Gets a brick by ID, or null if not registered.
    /// </summary>
    Brick? GetBrick(string id);

    /// <summary>
    /// Brick IDs this registry knows about.
    /// </summary>
    IReadOnlyList<string> GetRegisteredIds();
}
