namespace Nexo.Core.Application.Skills.Models;

/// <summary>Result of a skill script approval request.</summary>
public enum NexoSkillApprovalStatus
{
    AutoApproved,
    Approved,
    Denied,
    TimedOut,
    Pending,
}
