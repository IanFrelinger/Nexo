using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Agents.Models;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Resolves dependencies between agents and manages execution order.
/// </summary>
public sealed class DependencyResolver
{
    private readonly ILogger<DependencyResolver> _logger;
    private readonly ConcurrentDictionary<string, AgentContainer> _agents = new();
    private readonly ConcurrentDictionary<string, object> _outputs = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _dependencyGraph = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _reverseDependencyGraph = new();

    public DependencyResolver(ILogger<DependencyResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers an agent and builds the dependency graph.
    /// </summary>
    public void RegisterAgent(AgentContainer container)
    {
        if (container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }

        var agentId = container.AgentId;
        _agents[agentId] = container;

        // Build dependency graph
        _dependencyGraph[agentId] = new HashSet<string>(container.Agent.Spec.Dependencies);
        
        // Build reverse dependency graph (who depends on this agent) - thread-safe
        foreach (var dep in container.Agent.Spec.Dependencies)
        {
            _reverseDependencyGraph.AddOrUpdate(
                dep,
                new HashSet<string> { agentId },
                (key, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add(agentId);
                    }
                    return existing;
                });
        }

        _logger.LogDebug("Registered agent {AgentId} with {DependencyCount} dependencies", 
            agentId, container.Agent.Spec.Dependencies.Count);
    }

    /// <summary>
    /// Gets agents that are ready to execute (all dependencies resolved).
    /// </summary>
    public IReadOnlyList<AgentContainer> GetReadyAgents()
    {
        return _agents.Values
            .Where(container =>
            {
                var agentId = container.AgentId;
                var dependencies = container.Agent.Spec.Dependencies;

                // Agent is ready if:
                // 1. All dependencies have outputs
                // 2. Agent is in Ready or Executing state (not Failed/Completed/Terminated)
                return dependencies.All(dep => _outputs.ContainsKey(dep)) &&
                       (container.State == AgentState.Ready || container.State == AgentState.Executing);
            })
            .ToList();
    }

    /// <summary>
    /// Gets agents that are blocked (waiting for dependencies).
    /// </summary>
    public IReadOnlyList<AgentContainer> GetBlockedAgents()
    {
        return _agents.Values
            .Where(container =>
            {
                var dependencies = container.Agent.Spec.Dependencies;
                var hasUnresolvedDependencies = dependencies.Any(dep => !_outputs.ContainsKey(dep));
                return hasUnresolvedDependencies && container.State != AgentState.Completed;
            })
            .ToList();
    }

    /// <summary>
    /// Records an agent's output and unblocks dependent agents.
    /// </summary>
    public void RecordOutput(string agentId, object output)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        }

        _outputs[agentId] = output ?? throw new ArgumentNullException(nameof(output));

        _logger.LogDebug("Recorded output for agent {AgentId}, unblocking dependent agents", agentId);

        // Notify dependent agents that this dependency is resolved
        if (_reverseDependencyGraph.TryGetValue(agentId, out var dependents))
        {
            foreach (var dependentId in dependents)
            {
                if (_agents.TryGetValue(dependentId, out var dependent))
                {
                    var dependencyOutputs = GetDependencyOutputs(dependent.Agent.Spec.Dependencies);
                    _ = dependent.Agent.WaitForDependenciesAsync(dependencyOutputs);
                }
            }
        }
    }

    /// <summary>
    /// Gets outputs for all dependencies of an agent.
    /// </summary>
    public IReadOnlyDictionary<string, object> GetDependencyOutputs(IReadOnlyList<string> dependencies)
    {
        var result = new Dictionary<string, object>();
        foreach (var dep in dependencies)
        {
            if (_outputs.TryGetValue(dep, out var output))
            {
                result[dep] = output;
            }
        }
        return result;
    }

    /// <summary>
    /// Gets the execution order (topological sort) for all agents.
    /// </summary>
    public IReadOnlyList<string> GetExecutionOrder()
    {
        var result = new List<string>();
        var visited = new HashSet<string>();
        var inProgress = new HashSet<string>();

        void Visit(string agentId)
        {
            if (visited.Contains(agentId))
            {
                return;
            }

            if (inProgress.Contains(agentId))
            {
                _logger.LogWarning("Circular dependency detected involving agent {AgentId}", agentId);
                return;
            }

            inProgress.Add(agentId);

            if (_dependencyGraph.TryGetValue(agentId, out var dependencies))
            {
                foreach (var dep in dependencies)
                {
                    if (_agents.ContainsKey(dep))
                    {
                        Visit(dep);
                    }
                }
            }

            inProgress.Remove(agentId);
            visited.Add(agentId);
            result.Add(agentId);
        }

        foreach (var agentId in _agents.Keys)
        {
            Visit(agentId);
        }

        return result;
    }

    /// <summary>
    /// Checks if all dependencies for an agent are resolved.
    /// </summary>
    public bool AreDependenciesResolved(string agentId)
    {
        if (!_agents.TryGetValue(agentId, out var container))
        {
            return false;
        }

        return container.Agent.Spec.Dependencies.All(dep => _outputs.ContainsKey(dep));
    }

    /// <summary>
    /// Gets all resolved outputs.
    /// </summary>
    public IReadOnlyDictionary<string, object> GetAllOutputs()
    {
        return _outputs;
    }

    /// <summary>
    /// Clears all registered agents and outputs.
    /// </summary>
    public void Clear()
    {
        _agents.Clear();
        _outputs.Clear();
        _dependencyGraph.Clear();
        _reverseDependencyGraph.Clear();
    }
}

