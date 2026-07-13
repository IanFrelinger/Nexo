using Nexo.Core.Application.Skills.Models;

namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Resolves script execution approval via policy auto-approve or human-in-the-loop gate.
/// </summary>
public interface INexoSkillApprovalResolver
{
    Task<NexoSkillApprovalStatus> ResolveScriptApprovalAsync(
        SkillScriptApprovalKey key,
        string description,
        CancellationToken cancellationToken = default);
}
