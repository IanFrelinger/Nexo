using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Negotiation.Models;
using System.Text.Json;

namespace Nexo.Orchestration.Negotiation;

/// <summary>
/// Full negotiation protocol with state machine for autonomous conflict resolution.
/// </summary>
public sealed class NegotiationProtocol
{
    private readonly ILogger<NegotiationProtocol> _logger;
    private readonly SchemaAdapter _schemaAdapter;
    private readonly ParetoOptimizer _paretoOptimizer;
    private readonly ConstraintRelaxer _constraintRelaxer;
    private readonly SynthesisEngine _synthesisEngine;
    private readonly IModel? _model;

    private const int MaxNegotiationRounds = 5;

    public NegotiationProtocol(
        ILogger<NegotiationProtocol> logger,
        SchemaAdapter schemaAdapter,
        ParetoOptimizer paretoOptimizer,
        ConstraintRelaxer constraintRelaxer,
        SynthesisEngine synthesisEngine,
        IModel? model = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schemaAdapter = schemaAdapter ?? throw new ArgumentNullException(nameof(schemaAdapter));
        _paretoOptimizer = paretoOptimizer ?? throw new ArgumentNullException(nameof(paretoOptimizer));
        _constraintRelaxer = constraintRelaxer ?? throw new ArgumentNullException(nameof(constraintRelaxer));
        _synthesisEngine = synthesisEngine ?? throw new ArgumentNullException(nameof(synthesisEngine));
        _model = model;
    }

    /// <summary>
    /// Main negotiation entry point - routes to appropriate strategy based on conflict type.
    /// </summary>
    public async Task<NegotiationResult> NegotiateAsync(
        Conflict conflict,
        IReadOnlyList<AgentContainer> agents,
        CancellationToken cancellationToken = default)
    {
        return conflict.ConflictType switch
        {
            ConflictType.Schema => await NegotiateSchemaConflictAsync(conflict, agents, cancellationToken),
            ConflictType.Resource => await NegotiateResourceConflictAsync(conflict, agents, cancellationToken),
            ConflictType.Constraint => await NegotiateConstraintConflictAsync(conflict, agents, cancellationToken),
            ConflictType.Philosophy => await NegotiatePhilosophyConflictAsync(conflict, agents, cancellationToken),
            _ => NegotiationResult.Escalated($"Unknown conflict type: {conflict.ConflictType}")
        };
    }

    /// <summary>
    /// Negotiates schema conflicts by merging schemas.
    /// </summary>
    private Task<NegotiationResult> NegotiateSchemaConflictAsync(
        Conflict conflict,
        IReadOnlyList<AgentContainer> agents,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Negotiating schema conflict between agents: {AgentIds}",
            string.Join(", ", conflict.AgentIds));

        var involvedAgents = agents
            .Where(a => conflict.AgentIds.Contains(a.AgentId))
            .ToList();

        if (involvedAgents.Count < 2)
        {
            return Task.FromResult(NegotiationResult.Escalated("Not all involved agents found"));
        }

        var agent1 = involvedAgents[0];
        var agent2 = involvedAgents[1];

        var schema1 = agent1.Agent.Spec.OutputSchema;
        var schema2 = agent2.Agent.Spec.OutputSchema;

        if (!schema1.HasValue || !schema2.HasValue)
        {
            return Task.FromResult(NegotiationResult.Escalated("One or both agents missing output schemas"));
        }

        var mergedSchema = _schemaAdapter.MergeSchemas(schema1.Value, schema2.Value);

        if (mergedSchema == null)
        {
            return Task.FromResult(NegotiationResult.Escalated("Could not merge schemas - incompatible types"));
        }

        _logger.LogInformation("Successfully merged schemas for agents {Agent1} and {Agent2}",
            agent1.AgentId, agent2.AgentId);

        return Task.FromResult(NegotiationResult.SchemaResolved(mergedSchema.Value, "Schemas merged into canonical form"));
    }

    /// <summary>
    /// Negotiates resource conflicts using Pareto optimization.
    /// </summary>
    private async Task<NegotiationResult> NegotiateResourceConflictAsync(
        Conflict conflict,
        IReadOnlyList<AgentContainer> agents,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Negotiating resource conflict between agents: {AgentIds}",
            string.Join(", ", conflict.AgentIds));

        var involvedAgents = agents
            .Where(a => conflict.AgentIds.Contains(a.AgentId))
            .ToList();

        // Get positions for negotiation flow
        var positions = await GetPositionsAsync(involvedAgents, conflict, cancellationToken);

        // Use Pareto optimizer to find optimal allocation
        var budget = new ResourceBudget
        {
            MaxComputeSeconds = 3600,
            MaxMemoryMB = 16384,
            MaxContextTokens = 200000
        };

        var paretoResult = _paretoOptimizer.Optimize(involvedAgents, budget);

        if (!paretoResult.HasConflict)
        {
            return NegotiationResult.ResourceResolved(
                new ResourceAllocation { Allocations = new Dictionary<string, AllocatedResources>() },
                "No resource conflict detected");
        }

        if (paretoResult.RecommendedAllocation == null)
        {
            return NegotiationResult.Escalated("Could not find valid resource allocation");
        }

        return NegotiationResult.ResourceResolved(
            paretoResult.RecommendedAllocation,
            $"Pareto-optimal allocation found with {paretoResult.ParetoFrontier.Count} options");
    }

    /// <summary>
    /// Negotiates constraint conflicts using relaxation.
    /// </summary>
    private async Task<NegotiationResult> NegotiateConstraintConflictAsync(
        Conflict conflict,
        IReadOnlyList<AgentContainer> agents,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Negotiating constraint conflict between agents: {AgentIds}",
            string.Join(", ", conflict.AgentIds));

        var involvedAgents = agents
            .Where(a => conflict.AgentIds.Contains(a.AgentId))
            .ToList();

        var positions = await GetPositionsAsync(involvedAgents, conflict, cancellationToken);

        var relaxationResult = _constraintRelaxer.Relax(positions, conflict);

        if (!relaxationResult.Success)
        {
            return NegotiationResult.Escalated(relaxationResult.FailureReason ?? "Constraint relaxation failed");
        }

        var resolution = new ProposedResolution
        {
            ProposerId = "constraint-relaxer",
            Description = relaxationResult.Explanation ?? "Constraints relaxed",
            RequiredChanges = relaxationResult.RelaxedConstraints,
            Confidence = 0.7
        };

        return NegotiationResult.Negotiated(resolution, relaxationResult.Explanation ?? "Constraints relaxed");
    }

    /// <summary>
    /// Negotiates philosophy conflicts using synthesis.
    /// </summary>
    private async Task<NegotiationResult> NegotiatePhilosophyConflictAsync(
        Conflict conflict,
        IReadOnlyList<AgentContainer> agents,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Negotiating philosophy conflict between agents: {AgentIds}",
            string.Join(", ", conflict.AgentIds));

        var involvedAgents = agents
            .Where(a => conflict.AgentIds.Contains(a.AgentId))
            .ToList();

        var positions = await GetPositionsAsync(involvedAgents, conflict, cancellationToken);

        // Run full negotiation flow with synthesis
        return await RunNegotiationFlowAsync(
            conflict,
            involvedAgents,
            async (pos) =>
            {
                // Try synthesis first
                var synthesis = await _synthesisEngine.SynthesizeAsync(pos, conflict, cancellationToken);
                return synthesis;
            },
            cancellationToken);
    }

    /// <summary>
    /// Full negotiation flow with articulation → impact analysis → proposals → synthesis.
    /// </summary>
    private async Task<NegotiationResult> RunNegotiationFlowAsync(
        Conflict conflict,
        IReadOnlyList<AgentContainer> involvedAgents,
        Func<NegotiationPosition[], Task<ProposedResolution?>> proposalGenerator,
        CancellationToken cancellationToken)
    {
        // Phase 1: Articulation - agents state their positions
        var positions = await GetPositionsAsync(involvedAgents, conflict, cancellationToken);

        // Phase 2: Impact Analysis - determine yield order
        var impacts = await AnalyzeImpactsAsync(positions, conflict, cancellationToken);
        var yieldOrder = impacts.OrderBy(i => i.ImpactScore).Select(i => i.AgentId).ToList();

        // Phase 3: Proposal Rounds
        for (int round = 0; round < MaxNegotiationRounds; round++)
        {
            var proposerIndex = round % yieldOrder.Count;
            var proposer = involvedAgents.First(a => a.AgentId == yieldOrder[proposerIndex]);

            var proposal = await proposalGenerator(positions.ToArray());

            if (proposal == null) continue;

            if (await AllAcceptAsync(involvedAgents, proposal, cancellationToken))
            {
                return NegotiationResult.Negotiated(proposal, $"Accepted in round {round + 1}", round + 1);
            }
        }

        // Phase 4: Synthesis - creative resolution attempt
        var synthesis = await _synthesisEngine.SynthesizeAsync(positions, conflict, cancellationToken);
        if (synthesis != null && await AllAcceptAsync(involvedAgents, synthesis, cancellationToken))
        {
            return NegotiationResult.Synthesized(synthesis, "Creative synthesis accepted");
        }

        // Phase 5: Escalation
        return NegotiationResult.Escalated("Negotiation failed after all attempts");
    }

    /// <summary>
    /// Extracts negotiation positions from agents.
    /// </summary>
    private Task<IReadOnlyList<NegotiationPosition>> GetPositionsAsync(
        IReadOnlyList<AgentContainer> agents,
        Conflict conflict,
        CancellationToken cancellationToken)
    {
        var positions = new List<NegotiationPosition>();

        foreach (var agent in agents)
        {
            var spec = agent.Agent.Spec;

            // Extract hard constraints
            var hardConstraints = spec.Constraints
                .Where(c => c.IsMandatory)
                .Select(c => c.Description)
                .ToList();

            // Extract soft constraints with flexibility
            var softConstraints = spec.Constraints
                .Where(c => !c.IsMandatory)
                .ToDictionary(
                    c => c.Description,
                    c => CalculateFlexibility(c.Description, spec));

            // Calculate overall flexibility
            var flexibilityScore = CalculateOverallFlexibility(spec);

            var position = new NegotiationPosition
            {
                AgentId = spec.AgentId,
                Domain = spec.Domain,
                PrimaryGoal = spec.Goal,
                UnderlyingGoals = ExtractUnderlyingGoals(spec),
                HardConstraints = hardConstraints,
                SoftConstraints = softConstraints,
                Resources = spec.ResourceRequirements,
                FlexibilityScore = flexibilityScore
            };

            positions.Add(position);
        }

        return Task.FromResult<IReadOnlyList<NegotiationPosition>>(positions);
    }

    /// <summary>
    /// Analyzes impact of each agent yielding.
    /// </summary>
    private Task<IReadOnlyList<ImpactAnalysis>> AnalyzeImpactsAsync(
        IReadOnlyList<NegotiationPosition> positions,
        Conflict conflict,
        CancellationToken cancellationToken)
    {
        var impacts = new List<ImpactAnalysis>();

        foreach (var position in positions)
        {
            // Impact score based on:
            // - Number of dependencies on this agent
            // - Criticality of constraints
            // - Resource requirements
            var impactScore = CalculateImpactScore(position, positions);

            impacts.Add(new ImpactAnalysis
            {
                AgentId = position.AgentId,
                ImpactScore = impactScore,
                ImpactDescription = $"Agent {position.AgentId} has {impactScore:P0} impact if it yields"
            });
        }

        return Task.FromResult<IReadOnlyList<ImpactAnalysis>>(impacts);
    }

    /// <summary>
    /// Checks if all agents accept a proposal.
    /// </summary>
    private Task<bool> AllAcceptAsync(
        IReadOnlyList<AgentContainer> agents,
        ProposedResolution proposal,
        CancellationToken cancellationToken)
    {
        // Simplified acceptance logic:
        // - Check if proposal satisfies hard constraints
        // - Check if required changes are acceptable based on flexibility
        foreach (var agent in agents)
        {
            var spec = agent.Agent.Spec;

            // Check if proposal violates hard constraints
            if (proposal.RequiredChanges.TryGetValue(agent.AgentId, out var change))
            {
                var hardConstraints = spec.Constraints.Where(c => c.IsMandatory).ToList();
                foreach (var constraint in hardConstraints)
                {
                    // Simple check: if change contradicts constraint, reject
                    if (IsContradictory(change, constraint.Description))
                    {
                        _logger.LogDebug("Agent {AgentId} rejects proposal due to hard constraint violation",
                            agent.AgentId);
                        return Task.FromResult(false);
                    }
                }
            }
        }

        // All agents accept (simplified - in reality would use agent evaluation)
        return Task.FromResult(true);
    }

    private static double CalculateFlexibility(string constraint, AgentSpawnSpec spec)
    {
        // Flexibility based on constraint language
        var flexibilityKeywords = new[] { "prefer", "ideally", "should", "recommended" };
        var rigidKeywords = new[] { "must", "required", "critical", "essential" };

        var lower = constraint.ToLowerInvariant();
        if (rigidKeywords.Any(k => lower.Contains(k)))
        {
            return 0.2; // Low flexibility
        }
        if (flexibilityKeywords.Any(k => lower.Contains(k)))
        {
            return 0.7; // High flexibility
        }
        return 0.5; // Medium flexibility
    }

    private static double CalculateOverallFlexibility(AgentSpawnSpec spec)
    {
        // Average flexibility of soft constraints
        var softConstraints = spec.Constraints.Where(c => !c.IsMandatory).ToList();
        if (!softConstraints.Any())
        {
            return 0.3; // Low flexibility if no soft constraints
        }

        var avgFlexibility = softConstraints
            .Select(c => CalculateFlexibility(c.Description, spec))
            .Average();

        return avgFlexibility;
    }

    private static IReadOnlyList<string> ExtractUnderlyingGoals(AgentSpawnSpec spec)
    {
        // Extract underlying goals from description and goal
        var text = $"{spec.Goal} {spec.Description}".ToLowerInvariant();
        var goals = new List<string>();

        // Look for goal indicators
        if (text.Contains("performance") || text.Contains("fast") || text.Contains("speed"))
        {
            goals.Add("optimize performance");
        }
        if (text.Contains("quality") || text.Contains("thorough") || text.Contains("accurate"))
        {
            goals.Add("ensure quality");
        }
        if (text.Contains("simple") || text.Contains("minimal") || text.Contains("clean"))
        {
            goals.Add("maintain simplicity");
        }
        if (text.Contains("scalable") || text.Contains("scale") || text.Contains("growth"))
        {
            goals.Add("enable scalability");
        }

        return goals;
    }

    private static double CalculateImpactScore(
        NegotiationPosition position,
        IReadOnlyList<NegotiationPosition> allPositions)
    {
        // Impact based on:
        // - Number of hard constraints (more = higher impact)
        // - Resource requirements (more = higher impact)
        // - Flexibility (less flexible = higher impact)

        var constraintImpact = position.HardConstraints.Count * 0.3;
        var resourceImpact = position.Resources != null
            ? Math.Min(0.4, (position.Resources.EstimatedComputeSeconds ?? 0) / 10000.0)
            : 0.0;
        var flexibilityImpact = (1.0 - position.FlexibilityScore) * 0.3;

        return Math.Min(1.0, constraintImpact + resourceImpact + flexibilityImpact);
    }

    private static bool IsContradictory(string change, string constraint)
    {
        var changeLower = change.ToLowerInvariant();
        var constraintLower = constraint.ToLowerInvariant();

        var contradictions = new[]
        {
            ("fast", "slow"),
            ("synchronous", "asynchronous"),
            ("stateless", "state"),
            ("real-time", "batch")
        };

        foreach (var (word1, word2) in contradictions)
        {
            if ((changeLower.Contains(word1) && constraintLower.Contains(word2)) ||
                (changeLower.Contains(word2) && constraintLower.Contains(word1)))
            {
                return true;
            }
        }

        return false;
    }
}
