using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Coordination.Conflicts;
using System.Text.Json;

namespace Nexo.Orchestration.Negotiation;

/// <summary>
/// Basic negotiation protocol for resolving schema conflicts between agents.
/// </summary>
public sealed class NegotiationProtocol
{
    private readonly ILogger<NegotiationProtocol> _logger;
    private readonly SchemaAdapter _schemaAdapter;

    public NegotiationProtocol(ILogger<NegotiationProtocol> logger, SchemaAdapter? schemaAdapter = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schemaAdapter = schemaAdapter ?? new SchemaAdapter(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SchemaAdapter>.Instance);
    }

    /// <summary>
    /// Attempts to negotiate a resolution for a conflict.
    /// </summary>
    public Task<NegotiationResult> NegotiateAsync(
        Conflict conflict,
        IReadOnlyList<AgentContainer> agents,
        CancellationToken cancellationToken = default)
    {
        if (conflict == null)
        {
            throw new ArgumentNullException(nameof(conflict));
        }

        if (conflict.ConflictType != ConflictType.Schema)
        {
            // Only handle schema conflicts for now
            return Task.FromResult(new NegotiationResult
            {
                Success = false,
                Reason = $"Conflict type {conflict.ConflictType} not supported by basic negotiation"
            });
        }

        _logger.LogInformation("Negotiating schema conflict between agents: {AgentIds}",
            string.Join(", ", conflict.AgentIds));

        // Get the agents involved in the conflict
        var involvedAgents = agents
            .Where(a => conflict.AgentIds.Contains(a.AgentId))
            .ToList();

        if (involvedAgents.Count < 2)
        {
            return Task.FromResult(new NegotiationResult
            {
                Success = false,
                Reason = "Not all involved agents found"
            });
        }

        // Attempt to create a canonical schema that both agents can use
        var agent1 = involvedAgents[0];
        var agent2 = involvedAgents[1];

        var schema1 = agent1.Agent.Spec.OutputSchema;
        var schema2 = agent2.Agent.Spec.OutputSchema;

        if (!schema1.HasValue || !schema2.HasValue)
        {
            return Task.FromResult(new NegotiationResult
            {
                Success = false,
                Reason = "One or both agents missing output schemas"
            });
        }

        // Use SchemaAdapter to create a merged schema
        var mergedSchema = _schemaAdapter.MergeSchemas(schema1.Value, schema2.Value);

        if (mergedSchema == null)
        {
            return Task.FromResult(new NegotiationResult
            {
                Success = false,
                Reason = "Could not merge schemas - incompatible types"
            });
        }

        _logger.LogInformation("Successfully merged schemas for agents {Agent1} and {Agent2}",
            agent1.AgentId, agent2.AgentId);

        return Task.FromResult(new NegotiationResult
        {
            Success = true,
            ResolvedSchema = mergedSchema,
            ResolutionStrategy = "SchemaMerge",
            Reason = "Schemas merged into canonical form"
        });
    }
}

/// <summary>
/// Result of a negotiation attempt.
/// </summary>
public sealed record NegotiationResult
{
    public bool Success { get; init; }
    public JsonElement? ResolvedSchema { get; init; }
    public string? ResolutionStrategy { get; init; }
    public string? Reason { get; init; }
}

