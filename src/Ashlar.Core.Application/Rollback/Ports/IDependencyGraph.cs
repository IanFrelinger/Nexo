namespace Ashlar.Core.Application.Rollback.Ports;

/// <summary>
/// Tracks dependencies between components (bricks, agents, behaviors).
/// Used by rollback to determine transitive impact.
/// </summary>
public interface IDependencyGraph
{
    void Register(string componentId, IEnumerable<string> dependsOn);
    IEnumerable<string> GetDependents(string componentId);
    IEnumerable<string> GetDependencies(string componentId);
    IEnumerable<string> GetTransitiveDependents(string componentId);
    void Remove(string componentId);
    bool HasCycles();
}
