namespace Ashlar.Core.Application.Trust.Models;

/// <summary>
/// Unified audit entry for data decisions: sanitization, boundary changes, or classification.
/// </summary>
public sealed class DataDecisionAuditEntry
{
    /// <summary>Event type: Sanitization, BoundaryChange, or Classification.</summary>
    public required string EventType { get; init; }

    /// <summary>When the event occurred.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Product Fleet tenant scope when the event is tenant-bound (e.g. copilot jobs).</summary>
    public string? TenantId { get; init; }

    /// <summary>Sanitization rule version that produced this disposition.</summary>
    public string? RuleVersion { get; init; }

    /// <summary>Field or data type that was sanitized.</summary>
    public string? FieldOrType { get; init; }

    /// <summary>Sanitization disposition (redacted, dropped, allowed, etc.).</summary>
    public string? Disposition { get; init; }

    /// <summary>Reason for the sanitization decision.</summary>
    public string? Reason { get; init; }

    /// <summary>Boundary change type (category, source, project, pause).</summary>
    public string? ChangeType { get; init; }

    /// <summary>Data category affected by a boundary change.</summary>
    public string? Category { get; init; }

    /// <summary>Source identifier affected by a boundary change.</summary>
    public string? SourceId { get; init; }

    /// <summary>Project path affected by a boundary change.</summary>
    public string? ProjectPath { get; init; }

    /// <summary>Previous boundary state before the change.</summary>
    public string? PreviousState { get; init; }

    /// <summary>New boundary state after the change.</summary>
    public string? NewState { get; init; }

    /// <summary>Classified data type label.</summary>
    public string? DataType { get; init; }

    /// <summary>Assigned sensitivity or trust level name.</summary>
    public string? LevelName { get; init; }
}
