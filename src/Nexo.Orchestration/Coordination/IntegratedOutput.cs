using Microsoft.Extensions.Logging;
using System.Text.Json;
using Nexo.Orchestration.Architect.Models;

namespace Nexo.Orchestration.Coordination;

/// <summary>
/// Integrated output from multiple agents.
/// 
/// Contains:
/// - Integrated results organized by domain
/// - Raw outputs from individual agents
/// - Integration and validation errors
/// - Integration timestamp
/// - Validity flag
/// 
/// Produced by OutputIntegrator.Integrate() after merging and validating agent outputs.
/// </summary>
public sealed record IntegratedOutput
{
    /// <summary>
    /// Integrated results organized by domain.
    /// </summary>
    public required IReadOnlyDictionary<string, object> IntegratedResults { get; init; }

    /// <summary>
    /// Raw outputs from individual agents.
    /// </summary>
    public required IReadOnlyDictionary<string, object> AgentOutputs { get; init; }

    /// <summary>
    /// Errors encountered during integration.
    /// </summary>
    public IReadOnlyList<string> IntegrationErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Validation errors in the integrated output.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Timestamp when integration was performed.
    /// </summary>
    public DateTimeOffset IntegratedAt { get; init; }

    /// <summary>
    /// Whether the integration is valid (no errors).
    /// </summary>
    public bool IsValid => IntegrationErrors.Count == 0 && ValidationErrors.Count == 0;
}
