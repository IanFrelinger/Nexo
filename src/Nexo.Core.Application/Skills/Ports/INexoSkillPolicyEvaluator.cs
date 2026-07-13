using Nexo.Core.Application.Skills.Models;

namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Evaluates skill visibility and script governance from the active trust policy pack.
/// </summary>
public interface INexoSkillPolicyEvaluator
{
    bool IsSkillVisible(NexoSkillDescriptor skill, NexoSkillExecutionContext context);

    bool IsScriptAllowed(string skillName, string scriptPath, NexoSkillExecutionContext context);

    bool IsScriptAutoApproved(string skillName, string scriptPath, NexoSkillExecutionContext context);

    NexoScriptRunOptions GetScriptLimits(NexoSkillExecutionContext context);

    SkillPolicyRules? GetActiveSkillRules();
}
