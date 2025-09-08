using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Specialized;

namespace Nexo.Feature.AI.Services;

/// <summary>
/// Registry for managing specialized AI agents
/// </summary>
public class SpecializedAgentRegistry : ISpecializedAgentRegistry
{
    private readonly Dictionary<string, ISpecializedAgent> _agents = new();
    private readonly ILogger<SpecializedAgentRegistry> _logger;
    
    public SpecializedAgentRegistry(
        IEnumerable<ISpecializedAgent> agents,
        ILogger<SpecializedAgentRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Register all provided agents
        foreach (var agent in agents ?? Enumerable.Empty<ISpecializedAgent>())
        {
            RegisterAgent(agent);
        }
        
        _logger.LogInformation("Specialized agent registry initialized with {AgentCount} agents", _agents.Count);
    }
    
    public IEnumerable<ISpecializedAgent> GetAllAgents()
    {
        return _agents.Values.ToList();
    }
    
    public IEnumerable<ISpecializedAgent> GetAgentsBySpecialization(AgentSpecialization specialization)
    {
        return _agents.Values
            .Where(agent => agent.Specialization.HasFlag(specialization))
            .ToList();
    }
    
    public ISpecializedAgent? GetAgentById(string agentId)
    {
        return _agents.TryGetValue(agentId, out var agent) ? agent : null;
    }
    
    public void RegisterAgent(ISpecializedAgent agent)
    {
        if (agent == null)
        {
            throw new ArgumentNullException(nameof(agent));
        }
        
        if (string.IsNullOrEmpty(agent.AgentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agent));
        }
        
        if (_agents.ContainsKey(agent.AgentId))
        {
            _logger.LogWarning("Agent with ID {AgentId} is already registered, replacing with new instance", agent.AgentId);
        }
        
        _agents[agent.AgentId] = agent;
        _logger.LogDebug("Registered agent {AgentId} with specializations {Specializations}", 
            agent.AgentId, agent.Specialization);
    }
    
    public void UnregisterAgent(string agentId)
    {
        if (string.IsNullOrEmpty(agentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        }
        
        if (_agents.Remove(agentId))
        {
            _logger.LogDebug("Unregistered agent {AgentId}", agentId);
        }
        else
        {
            _logger.LogWarning("Attempted to unregister non-existent agent {AgentId}", agentId);
        }
    }
    
    public IEnumerable<ISpecializedAgent> GetAgentsByPlatform(PlatformCompatibility platform)
    {
        return _agents.Values
            .Where(agent => agent.PlatformExpertise.HasFlag(platform) || 
                           agent.PlatformExpertise.HasFlag(PlatformCompatibility.All))
            .ToList();
    }
    
    public IEnumerable<ISpecializedAgent> GetAgentsByOptimizationTarget(OptimizationTarget target)
    {
        return _agents.Values
            .Where(agent => agent.OptimizationProfile.PrimaryTarget == target ||
                           agent.OptimizationProfile.PrimaryTarget == OptimizationTarget.Balanced)
            .ToList();
    }
    
    public AgentRegistryStatistics GetRegistryStatistics()
    {
        var agents = _agents.Values.ToList();
        
        var specializationCounts = new Dictionary<AgentSpecialization, int>();
        var platformCounts = new Dictionary<PlatformCompatibility, int>();
        var optimizationTargetCounts = new Dictionary<OptimizationTarget, int>();
        
        foreach (var agent in agents)
        {
            // Count specializations
            foreach (AgentSpecialization specialization in Enum.GetValues<AgentSpecialization>())
            {
                if (agent.Specialization.HasFlag(specialization) && specialization != AgentSpecialization.None)
                {
                    specializationCounts[specialization] = specializationCounts.GetValueOrDefault(specialization, 0) + 1;
                }
            }
            
            // Count platforms
            foreach (PlatformCompatibility platform in Enum.GetValues<PlatformCompatibility>())
            {
                if (agent.PlatformExpertise.HasFlag(platform) && platform != PlatformCompatibility.None)
                {
                    platformCounts[platform] = platformCounts.GetValueOrDefault(platform, 0) + 1;
                }
            }
            
            // Count optimization targets
            var target = agent.OptimizationProfile.PrimaryTarget;
            optimizationTargetCounts[target] = optimizationTargetCounts.GetValueOrDefault(target, 0) + 1;
        }
        
        return new AgentRegistryStatistics
        {
            TotalAgents = agents.Count,
            SpecializationCounts = specializationCounts,
            PlatformCounts = platformCounts,
            OptimizationTargetCounts = optimizationTargetCounts,
            AgentsWithRealTimeOptimization = agents.Count(a => a.OptimizationProfile.SupportsRealTimeOptimization)
        };
    }
    
    public IEnumerable<ISpecializedAgent> FindBestAgentsForRequest(AgentRequest request)
    {
        var candidates = new List<(ISpecializedAgent Agent, double Score)>();
        
        foreach (var agent in _agents.Values)
        {
            var score = CalculateAgentScore(agent, request);
            if (score > 0.5) // Only consider agents with reasonable match
            {
                candidates.Add((agent, score));
            }
        }
        
        return candidates
            .OrderByDescending(c => c.Score)
            .Select(c => c.Agent)
            .ToList();
    }
    
    private double CalculateAgentScore(ISpecializedAgent agent, AgentRequest request)
    {
        var score = 0.0;
        
        // Check specialization match
        if (request.RequiredSpecialization.HasValue && 
            agent.Specialization.HasFlag(request.RequiredSpecialization.Value))
        {
            score += 0.4;
        }
        
        // Check platform match
        if (request.TargetPlatform.HasValue && 
            (agent.PlatformExpertise.HasFlag(request.TargetPlatform.Value) || 
             agent.PlatformExpertise.HasFlag(PlatformCompatibility.All)))
        {
            score += 0.3;
        }
        
        // Check performance requirements match
        if (request.PerformanceRequirements != null && 
            agent.OptimizationProfile.PrimaryTarget == request.PerformanceRequirements.PrimaryTarget)
        {
            score += 0.2;
        }
        
        // Check if agent supports real-time optimization when needed
        if (request.Context?.ContainsKey("RequiresRealTimeOptimization") == true &&
            agent.OptimizationProfile.SupportsRealTimeOptimization)
        {
            score += 0.1;
        }
        
        return Math.Min(score, 1.0);
    }
}

/// <summary>
/// Agent registry statistics
/// </summary>
public record AgentRegistryStatistics
{
    public int TotalAgents { get; init; }
    public Dictionary<AgentSpecialization, int> SpecializationCounts { get; init; } = new();
    public Dictionary<PlatformCompatibility, int> PlatformCounts { get; init; } = new();
    public Dictionary<OptimizationTarget, int> OptimizationTargetCounts { get; init; } = new();
    public int AgentsWithRealTimeOptimization { get; init; }
}
