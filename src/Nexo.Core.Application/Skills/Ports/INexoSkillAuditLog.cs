using Nexo.Core.Application.Skills.Models;

namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Emits structured skill disclosure audit events to configured sinks.
/// </summary>
public interface INexoSkillAuditLog
{
    void Log(NexoSkillAuditEvent auditEvent);
}
