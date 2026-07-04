using Nexo.Abstractions.Barriers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Abstractions.Execution;
using Nexo.Abstractions.Routing;
using Nexo.Abstractions.Transport;
using Nexo.Orchestration.Agents;
using Nexo.Orchestration.Architect;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Coordination.Conflicts;
using Nexo.Orchestration.Communication;
using Nexo.Orchestration.Communication.Models;
using Nexo.Orchestration.Metrics;
using Nexo.Orchestration.Negotiation;
using Nexo.Orchestration.Barriers;
using Nexo.Orchestration.Resilience;
using Nexo.Orchestration.Models;
using Nexo.Orchestration.Transport;
using Nexo.Core.Application.Common.Ports;

namespace Nexo.Orchestration.Coordination;

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
