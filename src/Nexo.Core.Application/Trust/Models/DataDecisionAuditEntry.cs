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

    /// <summary>Skill name for skill disclosure audit events.</summary>
    public string? SkillName { get; init; }

    /// <summary>Acting identity for skill disclosure audit events.</summary>
    public string? ActingIdentity { get; init; }

    /// <summary>Barrier level for skill disclosure audit events.</summary>
    public string? BarrierLevel { get; init; }

    /// <summary>Trust tier for skill disclosure audit events.</summary>
    public string? TrustTier { get; init; }

    /// <summary>Active policy pack id for skill disclosure audit events.</summary>
    public string? PolicyPackId { get; init; }

    /// <summary>Correlation id for skill disclosure audit events.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Resource path for skill resource-read events.</summary>
    public string? ResourcePath { get; init; }

    /// <summary>Script path for skill script events.</summary>
    public string? ScriptPath { get; init; }

    /// <summary>SHA-256 hash of script content for skill script events.</summary>
    public string? ScriptContentHash { get; init; }

    /// <summary>Execution outcome for skill script events.</summary>
    public string? Outcome { get; init; }

    /// <summary>Execution duration in milliseconds for skill script events.</summary>
    public long? DurationMs { get; init; }
}
