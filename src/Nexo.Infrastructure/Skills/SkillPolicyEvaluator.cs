using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.Infrastructure.Skills;

/// <summary>
/// Evaluates skill visibility and script governance from the active trust policy pack.
/// </summary>
public sealed class SkillPolicyEvaluator : INexoSkillPolicyEvaluator
{
    private static readonly NexoScriptRunOptions DefaultLimits = new(
        Timeout: TimeSpan.FromSeconds(30),
        MaxOutputBytes: 65_536,
        WorkingDirectoryRoot: Path.GetTempPath());

    private readonly ITrustPolicyPackRegistry _policyPackRegistry;

    /// <summary>Initializes a new skill policy evaluator.</summary>
    public SkillPolicyEvaluator(ITrustPolicyPackRegistry policyPackRegistry)
        => _policyPackRegistry = policyPackRegistry;

    /// <inheritdoc />
    public SkillPolicyRules? GetActiveSkillRules()
    {
        var active = _policyPackRegistry.GetActivePack();
        if (active == null)
            return null;

        return _policyPackRegistry.GetById(active.Id)?.SkillRules;
    }

    /// <inheritdoc />
    public bool IsSkillVisible(NexoSkillDescriptor skill, NexoSkillExecutionContext context)
    {
        var rules = ResolveRules(context);
        if (rules?.Visibility == null || rules.Visibility.Count == 0)
            return true;

        var tierKey = context.TrustTier.ToString();
        if (!rules.Visibility.TryGetValue(tierKey, out var tierRules))
            return false;

        if (MatchesAnyPattern(skill.Name, tierRules.Deny))
            return false;

        if (tierRules.Allow.Count == 0)
            return true;

        return MatchesAnyPattern(skill.Name, tierRules.Allow);
    }

    /// <inheritdoc />
    public bool IsScriptAllowed(string skillName, string scriptPath, NexoSkillExecutionContext context)
    {
        var rules = ResolveRules(context);
        if (rules?.AllowedScripts == null || rules.AllowedScripts.Count == 0)
            return true;

        if (!rules.AllowedScripts.TryGetValue(skillName, out var allowed))
            return false;

        return allowed.Any(pattern => MatchesScriptPattern(scriptPath, pattern));
    }

    /// <inheritdoc />
    public bool IsScriptAutoApproved(string skillName, string scriptPath, NexoSkillExecutionContext context)
    {
        var rules = ResolveRules(context);
        if (rules?.AutoApproveScripts == null)
            return false;

        var tier = context.TrustTier.ToString();
        return rules.AutoApproveScripts.Any(rule =>
            string.Equals(rule.Skill, skillName, StringComparison.OrdinalIgnoreCase)
            && MatchesScriptPattern(scriptPath, rule.Script)
            && rule.Tiers.Any(t => string.Equals(t, tier, StringComparison.OrdinalIgnoreCase)));
    }

    /// <inheritdoc />
    public NexoScriptRunOptions GetScriptLimits(NexoSkillExecutionContext context)
    {
        var rules = ResolveRules(context);
        if (rules?.ScriptLimits == null)
            return DefaultLimits;

        return new NexoScriptRunOptions(
            Timeout: TimeSpan.FromSeconds(Math.Max(1, rules.ScriptLimits.TimeoutSeconds)),
            MaxOutputBytes: Math.Max(1024, rules.ScriptLimits.MaxOutputBytes),
            WorkingDirectoryRoot: DefaultLimits.WorkingDirectoryRoot);
    }

    private SkillPolicyRules? ResolveRules(NexoSkillExecutionContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.PolicyPackId))
        {
            var pack = _policyPackRegistry.GetById(context.PolicyPackId);
            if (pack?.SkillRules != null)
                return pack.SkillRules;
        }

        return GetActiveSkillRules();
    }

    private static bool MatchesAnyPattern(string value, IReadOnlyList<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (string.Equals(pattern, "*", StringComparison.Ordinal))
                return true;

            if (pattern.EndsWith("*", StringComparison.Ordinal))
            {
                var prefix = pattern[..^1];
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesScriptPattern(string scriptPath, string pattern)
    {
        var normalizedScript = scriptPath.Replace('\\', '/');
        var normalizedPattern = pattern.Replace('\\', '/');
        return string.Equals(normalizedScript, normalizedPattern, StringComparison.OrdinalIgnoreCase)
            || normalizedScript.EndsWith('/' + normalizedPattern.TrimStart('/'), StringComparison.OrdinalIgnoreCase);
    }
}
