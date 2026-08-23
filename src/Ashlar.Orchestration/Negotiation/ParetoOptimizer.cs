using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Architect;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Negotiation;

/// <summary>
/// Resolves resource conflicts by finding Pareto-optimal allocations.
/// 
/// Responsibilities:
/// - Finds Pareto-optimal resource allocations (no agent can improve without another getting worse)
/// - Generates Pareto frontier (set of optimal allocations)
/// - Identifies tradeoffs between allocations
/// - Recommends best allocation based on priorities
/// 
/// Used by NegotiationProtocol to resolve resource conflicts between agents.
/// </summary>
public sealed class ParetoOptimizer
{
    private readonly ILogger<ParetoOptimizer> _logger;

    public ParetoOptimizer(ILogger<ParetoOptimizer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Finds Pareto-optimal resource allocations for conflicting agents.
    /// </summary>
    public ParetoResult Optimize(
        IReadOnlyList<AgentContainer> agents,
        ResourceBudget budget)
    {
        var requirements = agents
            .Select(a => (a.AgentId, a.Agent.Spec.ResourceRequirements ?? new ResourceRequirements()))
            .ToList();

        var totalCompute = requirements.Sum(r => r.Item2.EstimatedComputeSeconds ?? 0);
        var totalMemory = requirements.Sum(r => r.Item2.RequiredMemoryMB ?? 0);
        var totalContext = requirements.Sum(r => r.Item2.RequiredContextTokens ?? 0);

        // Check if within budget
        if (totalCompute <= (budget.MaxComputeSeconds ?? int.MaxValue) &&
            totalMemory <= (budget.MaxMemoryMB ?? int.MaxValue) &&
            totalContext <= (budget.MaxContextTokens ?? int.MaxValue))
        {
            return ParetoResult.NoConflict();
        }

        // Generate Pareto frontier - set of allocations where no agent can improve
        // without another agent getting worse
        var frontier = GenerateParetoFrontier(requirements, budget);

        return new ParetoResult
        {
            HasConflict = true,
            ParetoFrontier = frontier,
            RecommendedAllocation = frontier.FirstOrDefault(),
            Tradeoffs = GenerateTradeoffDescriptions(frontier)
        };
    }

    private IReadOnlyList<ResourceAllocation> GenerateParetoFrontier(
        List<(string AgentId, ResourceRequirements Requirements)> requirements,
        ResourceBudget budget)
    {
        var frontier = new List<ResourceAllocation>();

        // Base allocation: proportional scaling
        var totalCompute = requirements.Sum(r => r.Requirements.EstimatedComputeSeconds ?? 0);
        var totalMemory = requirements.Sum(r => r.Requirements.RequiredMemoryMB ?? 0);
        var totalContext = requirements.Sum(r => r.Requirements.RequiredContextTokens ?? 0);

        var computeScale = totalCompute > 0
            ? Math.Min(1.0, (budget.MaxComputeSeconds ?? int.MaxValue) / (double)totalCompute)
            : 1.0;
        var memoryScale = totalMemory > 0
            ? Math.Min(1.0, (budget.MaxMemoryMB ?? int.MaxValue) / (double)totalMemory)
            : 1.0;
        var contextScale = totalContext > 0
            ? Math.Min(1.0, (budget.MaxContextTokens ?? int.MaxValue) / (double)totalContext)
            : 1.0;

        // Use the most restrictive scale
        var scale = Math.Min(computeScale, Math.Min(memoryScale, contextScale));

        var baseAllocation = new ResourceAllocation
        {
            Allocations = requirements.ToDictionary(
                r => r.AgentId,
                r => new AllocatedResources
                {
                    ComputeSeconds = (int)((r.Requirements.EstimatedComputeSeconds ?? 0) * scale),
                    MemoryMb = (int)((r.Requirements.RequiredMemoryMB ?? 0) * scale),
                    ContextTokens = (int)((r.Requirements.RequiredContextTokens ?? 0) * scale)
                })
        };
        frontier.Add(baseAllocation);

        // Generate variations prioritizing different agents
        foreach (var (agentId, _) in requirements)
        {
            var prioritizedAllocation = GeneratePrioritizedAllocation(
                requirements, budget, agentId);
            if (prioritizedAllocation != null)
            {
                frontier.Add(prioritizedAllocation);
            }
        }

        return frontier;
    }

    private ResourceAllocation? GeneratePrioritizedAllocation(
        List<(string AgentId, ResourceRequirements Requirements)> requirements,
        ResourceBudget budget,
        string priorityAgentId)
    {
        var priorityReq = requirements.FirstOrDefault(r => r.AgentId == priorityAgentId);
        if (priorityReq == default)
        {
            return null;
        }

        // Give priority agent full allocation, scale others
        var priorityCompute = priorityReq.Requirements.EstimatedComputeSeconds ?? 0;
        var priorityMemory = priorityReq.Requirements.RequiredMemoryMB ?? 0;
        var priorityContext = priorityReq.Requirements.RequiredContextTokens ?? 0;

        var remainingCompute = (budget.MaxComputeSeconds ?? int.MaxValue) - priorityCompute;
        var remainingMemory = (budget.MaxMemoryMB ?? int.MaxValue) - priorityMemory;
        var remainingContext = (budget.MaxContextTokens ?? int.MaxValue) - priorityContext;

        if (remainingCompute < 0 || remainingMemory < 0 || remainingContext < 0)
        {
            // Priority agent alone exceeds budget - can't prioritize
            return null;
        }

        var otherRequirements = requirements.Where(r => r.AgentId != priorityAgentId).ToList();
        var otherTotalCompute = otherRequirements.Sum(r => r.Requirements.EstimatedComputeSeconds ?? 0);
        var otherTotalMemory = otherRequirements.Sum(r => r.Requirements.RequiredMemoryMB ?? 0);
        var otherTotalContext = otherRequirements.Sum(r => r.Requirements.RequiredContextTokens ?? 0);

        var otherComputeScale = otherTotalCompute > 0
            ? Math.Min(1.0, remainingCompute / (double)otherTotalCompute)
            : 1.0;
        var otherMemoryScale = otherTotalMemory > 0
            ? Math.Min(1.0, remainingMemory / (double)otherTotalMemory)
            : 1.0;
        var otherContextScale = otherTotalContext > 0
            ? Math.Min(1.0, remainingContext / (double)otherTotalContext)
            : 1.0;

        var otherScale = Math.Min(otherComputeScale, Math.Min(otherMemoryScale, otherContextScale));

        var allocations = new Dictionary<string, AllocatedResources>
        {
            [priorityAgentId] = new AllocatedResources
            {
                ComputeSeconds = priorityCompute,
                MemoryMb = priorityMemory,
                ContextTokens = priorityContext
            }
        };

        foreach (var (agentId, req) in otherRequirements)
        {
            allocations[agentId] = new AllocatedResources
            {
                ComputeSeconds = (int)((req.EstimatedComputeSeconds ?? 0) * otherScale),
                MemoryMb = (int)((req.RequiredMemoryMB ?? 0) * otherScale),
                ContextTokens = (int)((req.RequiredContextTokens ?? 0) * otherScale)
            };
        }

        return new ResourceAllocation { Allocations = allocations };
    }

    private IReadOnlyList<string> GenerateTradeoffDescriptions(
        IReadOnlyList<ResourceAllocation> frontier)
    {
        return frontier.Select((a, i) => $"Option {i + 1}: {DescribeAllocation(a)}").ToList();
    }

    private string DescribeAllocation(ResourceAllocation allocation)
    {
        var parts = allocation.Allocations
            .Select(kvp => $"{kvp.Key}: {kvp.Value.ComputeSeconds}s compute, {kvp.Value.MemoryMb}MB memory");
        return string.Join("; ", parts);
    }
}
