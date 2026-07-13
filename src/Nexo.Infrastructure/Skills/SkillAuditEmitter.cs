using System.Text.Json;
using Nexo.Abstractions.Barriers;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.Infrastructure.Skills;

/// <summary>
/// Emits skill audit events to barrier and data-decision audit sinks.
/// </summary>
public sealed class SkillAuditEmitter : INexoSkillAuditLog
{
    private readonly IBarrierAuditLog? _barrierAuditLog;
    private readonly IDataDecisionAuditLog? _dataDecisionAuditLog;

    /// <summary>Initializes a new skill audit emitter.</summary>
    public SkillAuditEmitter(
        IBarrierAuditLog? barrierAuditLog = null,
        IDataDecisionAuditLog? dataDecisionAuditLog = null)
    {
        _barrierAuditLog = barrierAuditLog;
        _dataDecisionAuditLog = dataDecisionAuditLog;
    }

    /// <inheritdoc />
    public void Log(NexoSkillAuditEvent auditEvent)
    {
        if (_barrierAuditLog != null)
        {
            _ = _barrierAuditLog.RecordAsync(new BarrierAuditEvent(
                auditEvent.EventType,
                auditEvent.BarrierLevel,
                auditEvent.ActingIdentity,
                auditEvent.SkillName,
                auditEvent.CorrelationId,
                string.Empty,
                auditEvent.OccurredAt,
                BuildDetail(auditEvent)));
        }

        _dataDecisionAuditLog?.LogSkillDisclosure(auditEvent);
    }

    private static string BuildDetail(NexoSkillAuditEvent auditEvent)
    {
        var payload = new Dictionary<string, object?>
        {
            ["trustTier"] = auditEvent.TrustTier,
            ["policyPackId"] = auditEvent.PolicyPackId,
            ["resourcePath"] = auditEvent.ResourcePath,
            ["scriptPath"] = auditEvent.ScriptPath,
            ["scriptContentHash"] = auditEvent.ScriptContentHash,
            ["outcome"] = auditEvent.Outcome,
            ["durationMs"] = auditEvent.DurationMs,
            ["detail"] = auditEvent.Detail,
        };

        return JsonSerializer.Serialize(payload);
    }
}
