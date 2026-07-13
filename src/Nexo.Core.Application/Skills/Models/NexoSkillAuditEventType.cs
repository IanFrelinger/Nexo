namespace Nexo.Core.Application.Skills.Models;

/// <summary>
/// Stable wire identifiers for skill disclosure audit events.
/// </summary>
public static class NexoSkillAuditEventType
{
    public static readonly string SkillAdvertised = "SKILL_ADVERTISED";
    public static readonly string SkillLoaded = "SKILL_LOADED";
    public static readonly string SkillResourceRead = "SKILL_RESOURCE_READ";
    public static readonly string SkillScriptApprovalRequested = "SKILL_SCRIPT_APPROVAL_REQUESTED";
    public static readonly string SkillScriptApprovalResolved = "SKILL_SCRIPT_APPROVAL_RESOLVED";
    public static readonly string SkillScriptExecuted = "SKILL_SCRIPT_EXECUTED";
}
