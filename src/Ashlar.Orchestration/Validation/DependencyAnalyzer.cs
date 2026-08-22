using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Validation;

/// <summary>
/// Analyzes dependencies between agents and detects cycles using topological sort.
/// 
/// Checks:
/// - Circular dependencies (cycles in dependency graph)
/// - Missing dependencies (agents that depend on non-existent agents)
/// - Dependency graph structure
/// 
/// Used by ArchitectAgent to validate decomposition results.
/// </summary>
public sealed class DependencyAnalyzer : IValidator
{
    private readonly ILogger<DependencyAnalyzer> _logger;

    public string Name => "DependencyAnalyzer";

    public DependencyAnalyzer(ILogger<DependencyAnalyzer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<ValidationError>> ValidateAsync(
        DecompositionResult result,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<ValidationError>();

        // Build dependency graph
        var graph = BuildDependencyGraph(result.Agents);
        
        // Detect cycles using topological sort
        var cycle = DetectCycle(graph);
        if (cycle != null)
        {
            errors.Add(new ValidationError
            {
                ErrorType = "Dependency",
                Message = $"Circular dependency detected: {string.Join(" -> ", cycle)} -> {cycle[0]}",
                Severity = ValidationSeverity.Error
            });
            _logger.LogWarning("Circular dependency detected: {Cycle}", string.Join(" -> ", cycle));
        }

        // Check for unreachable agents (agents that depend on non-existent agents)
        var agentIds = result.Agents.Select(a => a.AgentId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var agent in result.Agents)
        {
            foreach (var dep in GetEffectiveDependencies(agent))
            {
                if (!agentIds.Contains(dep))
                {
                    errors.Add(new ValidationError
                    {
                        ErrorType = "Dependency",
                        Message = $"Agent '{agent.AgentId}' depends on non-existent agent '{dep}'",
                        AgentId = agent.AgentId,
                        Severity = ValidationSeverity.Error
                    });
                }
            }
        }

        // Check for agents with excessive dependency depth (potential performance issue)
        var maxDepth = CalculateMaxDepth(graph);
        if (maxDepth > 10)
        {
            errors.Add(new ValidationError
            {
                ErrorType = "Dependency",
                Message = $"Dependency chain depth ({maxDepth}) exceeds recommended maximum (10). This may cause performance issues.",
                Severity = ValidationSeverity.Warning
            });
        }

        return Task.FromResult<IReadOnlyList<ValidationError>>(errors);
    }

    private Dictionary<string, List<string>> BuildDependencyGraph(IReadOnlyList<AgentSpawnSpec> agents)
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var agent in agents)
        {
            if (!graph.ContainsKey(agent.AgentId))
            {
                graph[agent.AgentId] = new List<string>();
            }

            foreach (var dep in GetEffectiveDependencies(agent))
            {
                graph[agent.AgentId].Add(dep);
            }
        }

        return graph;
    }

    private static IReadOnlyList<string> GetEffectiveDependencies(AgentSpawnSpec agent)
    {
        var dependencies = new HashSet<string>(
            agent.Dependencies.Where(dep => !string.IsNullOrWhiteSpace(dep)),
            StringComparer.OrdinalIgnoreCase);

        var supervisor = !string.IsNullOrWhiteSpace(agent.ReportsToAgentId)
            ? agent.ReportsToAgentId
            : agent.CommandChain.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

        if (!string.IsNullOrWhiteSpace(supervisor))
        {
            dependencies.Add(supervisor);
        }

        return dependencies.ToList();
    }

    private List<string>? DetectCycle(Dictionary<string, List<string>> graph)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string>();

        foreach (var node in graph.Keys)
        {
            if (!visited.Contains(node))
            {
                var cycle = DetectCycleDFS(node, graph, visited, recStack, path);
                if (cycle != null)
                {
                    return cycle;
                }
            }
        }

        return null;
    }

    private List<string>? DetectCycleDFS(
        string node,
        Dictionary<string, List<string>> graph,
        HashSet<string> visited,
        HashSet<string> recStack,
        List<string> path)
    {
        visited.Add(node);
        recStack.Add(node);
        path.Add(node);

        if (graph.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    var cycle = DetectCycleDFS(neighbor, graph, visited, recStack, path);
                    if (cycle != null)
                    {
                        return cycle;
                    }
                }
                else if (recStack.Contains(neighbor))
                {
                    // Found a cycle - return the cycle path
                    var cycleStart = path.IndexOf(neighbor);
                    var cycle = path.Skip(cycleStart).Append(neighbor).ToList();
                    return cycle;
                }
            }
        }

        recStack.Remove(node);
        path.RemoveAt(path.Count - 1);
        return null;
    }

    private int CalculateMaxDepth(Dictionary<string, List<string>> graph)
    {
        var maxDepth = 0;
        var depths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int CalculateDepth(string node)
        {
            // Check for cycles (should have been caught earlier, but protect against infinite recursion)
            if (visiting.Contains(node))
            {
                return 0; // Cycle detected, return 0 to break recursion
            }

            if (depths.TryGetValue(node, out var depth))
            {
                return depth;
            }

            if (!graph.TryGetValue(node, out var neighbors) || neighbors.Count == 0)
            {
                depths[node] = 0;
                return 0;
            }

            visiting.Add(node);
            try
            {
                var maxChildDepth = 0;
                foreach (var neighbor in neighbors)
                {
                    var childDepth = CalculateDepth(neighbor);
                    maxChildDepth = Math.Max(maxChildDepth, childDepth);
                }
                var nodeDepth = maxChildDepth + 1;
                depths[node] = nodeDepth;
                return nodeDepth;
            }
            finally
            {
                visiting.Remove(node);
            }
        }

        foreach (var node in graph.Keys)
        {
            if (!depths.ContainsKey(node))
            {
                var depth = CalculateDepth(node);
                maxDepth = Math.Max(maxDepth, depth);
            }
        }

        return maxDepth;
    }
}

