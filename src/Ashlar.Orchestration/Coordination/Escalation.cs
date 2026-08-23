using Microsoft.Extensions.Logging;
using Ashlar.Orchestration.Coordination.Conflicts;

namespace Ashlar.Orchestration.Coordination;

/// <summary>
/// Represents an escalation to human operators.
/// </summary>
public sealed class Escalation
{
    /// <summary>Unique escalation identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Related conflict, if escalation originated from one.</summary>
    public Conflict? Conflict { get; init; }

    /// <summary>Category label for the escalated issue.</summary>
    public string? IssueType { get; init; }

    /// <summary>Human-readable description of the issue.</summary>
    public string? Description { get; init; }

    /// <summary>UTC timestamp when escalation was created.</summary>
    public DateTimeOffset EscalatedAt { get; init; }

    /// <summary>Severity level assigned to the escalation.</summary>
    public EscalationSeverity Severity { get; init; }

    /// <summary>Additional context for operator review.</summary>
    public string? Context { get; init; }

    /// <summary>Current lifecycle status of the escalation.</summary>
    public EscalationStatus Status { get; set; }

    /// <summary>UTC timestamp when the escalation was resolved.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>Resolution notes or outcome summary.</summary>
    public string? Resolution { get; set; }
}
