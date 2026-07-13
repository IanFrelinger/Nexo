namespace Nexo.Core.Application.Skills.Models;

/// <summary>Skill governance rules embedded in a trust policy pack.</summary>
public sealed record SkillPolicyRules(
    IReadOnlyDictionary<string, SkillTierVisibilityRules> Visibility,
    IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedScripts,
    IReadOnlyList<SkillScriptAutoApprovalRule> AutoApproveScripts,
    SkillScriptLimits ScriptLimits);

/// <summary>Per-tier skill visibility allow/deny lists.</summary>
public sealed record SkillTierVisibilityRules(
    IReadOnlyList<string> Allow,
    IReadOnlyList<string> Deny);

/// <summary>Auto-approval tuple for (skill, script, tier).</summary>
public sealed record SkillScriptAutoApprovalRule(
    string Skill,
    string Script,
    IReadOnlyList<string> Tiers);

/// <summary>Default script execution limits from policy.</summary>
public sealed record SkillScriptLimits(
    int TimeoutSeconds,
    int MaxOutputBytes);
