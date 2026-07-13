using Nexo.Core.Application.Skills.Models;

namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Builds skill advertisement text for agent system prompts (stage 1 disclosure).
/// </summary>
public interface INexoSkillAgentBridge
{
    /// <summary>
    /// Returns markdown instructions listing visible skills for the acting context.
    /// </summary>
    Task<string> BuildSkillInstructionsAsync(
        NexoSkillExecutionContext context,
        CancellationToken cancellationToken = default);
}
