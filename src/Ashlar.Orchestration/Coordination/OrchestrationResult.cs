using Ashlar.Abstractions.Barriers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Abstractions.Execution;
using Ashlar.Abstractions.Routing;
using Ashlar.Abstractions.Transport;
using Ashlar.Orchestration.Agents;
using Ashlar.Orchestration.Architect;
using Ashlar.Orchestration.Architect.Models;
using Ashlar.Orchestration.Coordination.Conflicts;
using Ashlar.Orchestration.Communication;
using Ashlar.Orchestration.Communication.Models;
using Ashlar.Orchestration.Metrics;
using Ashlar.Orchestration.Negotiation;
using Ashlar.Orchestration.Barriers;
using Ashlar.Orchestration.Resilience;
using Ashlar.Orchestration.Models;
using Ashlar.Orchestration.Transport;
using Ashlar.Core.Application.Common.Ports;

namespace Ashlar.Orchestration.Coordination;

/// <summary>
/// Result of an orchestration run.
/// 
/// Contains the complete outcome of an orchestration request, including:
/// - Success status and integrated output
/// - Decomposition details
/// - Conflict information (all, resolved, unresolved)
/// - Escalations that occurred
/// - Progress summary
/// - Correlation ID for tracing
/// </summary>
public sealed record OrchestrationResult
{
    /// <summary>
    /// Gets a value indicating whether the orchestration succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the integrated output from all agents.
    /// </summary>
    public IntegratedOutput? IntegratedOutput { get; init; }

    /// <summary>
    /// Gets the decomposition result from the Architect agent.
    /// </summary>
    public DecompositionResult? Decomposition { get; init; }

    /// <summary>
    /// Gets all conflicts detected during orchestration.
    /// </summary>
    public IReadOnlyList<Conflict> Conflicts { get; init; } = Array.Empty<Conflict>();

    /// <summary>
    /// Gets conflicts that were successfully resolved.
    /// </summary>
    public IReadOnlyList<Conflict> ResolvedConflicts { get; init; } = Array.Empty<Conflict>();

    /// <summary>
    /// Gets conflicts that could not be resolved and were escalated.
    /// </summary>
    public IReadOnlyList<Conflict> UnresolvedConflicts { get; init; } = Array.Empty<Conflict>();

    /// <summary>
    /// Gets all escalations that occurred during orchestration.
    /// </summary>
    public IReadOnlyList<Escalation> Escalations { get; init; } = Array.Empty<Escalation>();

    /// <summary>
    /// Gets the progress summary for agent execution.
    /// </summary>
    public ProgressSummary? ProgressSummary { get; init; }

    /// <summary>
    /// Gets the correlation ID for tracing this orchestration run.
    /// </summary>
    public string? CorrelationId { get; init; }
}
