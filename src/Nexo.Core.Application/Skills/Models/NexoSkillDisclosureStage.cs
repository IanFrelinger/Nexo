namespace Nexo.Core.Application.Skills.Models;

/// <summary>Progressive disclosure stage for agent skills.</summary>
public enum NexoSkillDisclosureStage
{
    Advertised = 1,
    Loaded = 2,
    ResourceRead = 3,
    ScriptApprovalRequested = 4,
    ScriptApprovalResolved = 5,
    ScriptExecuted = 6,
}
