using Microsoft.Extensions.Logging;
using Nexo.Abstractions;
using Nexo.Orchestration.Architect.Models;
using Nexo.Orchestration.Playtest.Models;
using System.Text.Json;

namespace Nexo.Orchestration.Agents.Playtest;

/// <summary>
/// A request to change a design element.
/// 
/// Contains:
/// - Request ID and source issue ID
/// - Domain, change type, and target element
/// - Current and proposed values
/// - Rationale for the change
/// 
/// Used by FeedbackSynthesizerAgent to specify design changes.
/// </summary>
public sealed record DesignChangeRequest
{
    /// <summary>Unique change request identifier.</summary>
    public required string RequestId { get; init; }

    /// <summary>Balance or feedback issue that triggered the change.</summary>
    public required string SourceIssueId { get; init; }

    /// <summary>Design domain responsible for the element.</summary>
    public required string Domain { get; init; }

    /// <summary>Kind of design change requested.</summary>
    public required ChangeType ChangeType { get; init; }

    /// <summary>Game element targeted by the change.</summary>
    public required string TargetElement { get; init; }

    /// <summary>Current value before the proposed change.</summary>
    public string? CurrentValue { get; init; }

    /// <summary>Proposed new value for the element.</summary>
    public required string ProposedValue { get; init; }

    /// <summary>Reasoning supporting the proposed change.</summary>
    public required string Rationale { get; init; }
}
