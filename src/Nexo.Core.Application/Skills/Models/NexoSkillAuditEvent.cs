namespace Nexo.Core.Application.Skills.Models;

/// <summary>
/// Stable audit contract for skill disclosure and script execution.
/// </summary>
public sealed record NexoSkillAuditEvent(
    string EventType,
    string SkillName,
    string ActingIdentity,
    string BarrierLevel,
    string TrustTier,
    string? PolicyPackId,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    string? ResourcePath = null,
    string? ScriptPath = null,
    string? ScriptContentHash = null,
    string? Outcome = null,
    long? DurationMs = null,
    string? Detail = null);
