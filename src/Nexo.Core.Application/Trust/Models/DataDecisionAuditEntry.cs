namespace Nexo.Core.Application.Trust.Models;

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

    // Sanitization fields
    public string? RuleVersion { get; init; }
    public string? FieldOrType { get; init; }
    public string? Disposition { get; init; }
    public string? Reason { get; init; }

    // Boundary fields
    public string? ChangeType { get; init; }
    public string? Category { get; init; }
    public string? SourceId { get; init; }
    public string? ProjectPath { get; init; }
    public string? PreviousState { get; init; }
    public string? NewState { get; init; }

    // Classification fields
    public string? DataType { get; init; }
    public string? LevelName { get; init; }
}
