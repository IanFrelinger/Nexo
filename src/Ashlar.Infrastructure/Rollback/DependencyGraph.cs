using Ashlar.Core.Application.Rollback.Ports;

namespace Ashlar.Infrastructure.Rollback;

/// <summary>
/// In-memory DAG for tracking component dependencies.
/// </summary>
public sealed class DependencyGraph : IDependencyGraph
{
    private readonly Dictionary<string, HashSet<string>> _dependencies = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _dependents = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <summary>Registers .</summary>
    public void Register(string componentId, IEnumerable<string> dependsOn)
    {
        if (string.IsNullOrWhiteSpace(componentId))
            throw new ArgumentNullException(nameof(componentId));

        lock (_lock)
        {
            EnsureNode(componentId);
            foreach (var dep in dependsOn ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(dep)) continue;
                EnsureNode(dep);
                if (!_dependencies.TryGetValue(componentId, out var depSet))
                {
                    depSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _dependencies[componentId] = depSet;
                }
                depSet.Add(dep);
                if (!_dependents.TryGetValue(dep, out var depentSet))
                {
                    depentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _dependents[dep] = depentSet;
                }
                depentSet.Add(componentId);
            }
        }
    }

    /// <summary>Gets dependents.</summary>
    public IEnumerable<string> GetDependents(string componentId)
    {
        lock (_lock)
        {
            return _dependents.TryGetValue(componentId, out var set)
                ? set.ToList()
                : Array.Empty<string>();
        }
    }

    /// <summary>Gets dependencies.</summary>
    public IEnumerable<string> GetDependencies(string componentId)
    {
        lock (_lock)
        {
            return _dependencies.TryGetValue(componentId, out var set)
                ? set.ToList()
                : Array.Empty<string>();
        }
    }

    /// <summary>Gets transitive dependents.</summary>
    public IEnumerable<string> GetTransitiveDependents(string componentId)
    {
        lock (_lock)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<string>();
            stack.Push(componentId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!visited.Add(current)) continue;

                foreach (var dep in GetDependents(current))
                {
                    result.Add(dep);
                    stack.Push(dep);
                }
            }

            return result;
        }
    }

    /// <summary>Removes .</summary>
    public void Remove(string componentId)
    {
        lock (_lock)
        {
            if (_dependencies.TryGetValue(componentId, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (_dependents.TryGetValue(dep, out var depSet))
                        depSet.Remove(componentId);
                }
                _dependencies.Remove(componentId);
            }
            if (_dependents.TryGetValue(componentId, out var depents))
            {
                foreach (var d in depents)
                {
                    if (_dependencies.TryGetValue(d, out var dSet))
                        dSet.Remove(componentId);
                }
                _dependents.Remove(componentId);
            }
        }
    }

    /// <summary>Whether cycles exists.</summary>
    public bool HasCycles()
    {
        lock (_lock)
        {
            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in _dependencies.Keys) all.Add(k);
            foreach (var k in _dependents.Keys) all.Add(k);

            foreach (var start in all)
            {
                var path = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (HasCycle(start, path, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }
    }

    private bool HasCycle(string node, HashSet<string> path, HashSet<string> visited)
    {
        if (path.Contains(node)) return true;
        if (visited.Contains(node)) return false;

        visited.Add(node);
        path.Add(node);

        foreach (var dep in GetDependents(node))
        {
            if (HasCycle(dep, path, visited))
                return true;
        }

        path.Remove(node);
        return false;
    }

    private void EnsureNode(string id)
    {
        _dependencies.TryAdd(id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        _dependents.TryAdd(id, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }
}
