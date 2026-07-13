namespace Nexo.Core.Application.Skills.Models;

/// <summary>Pending human-in-the-loop approval for a skill script.</summary>
public sealed record NexoSkillApprovalRequest(
    string RequestId,
    SkillScriptApprovalKey Key,
    string Description,
    DateTimeOffset RequestedAt,
    NexoSkillApprovalStatus Status);
