using Nexo.Core.Application.Skills.Models;

namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Human-in-the-loop approval gate for skill script execution.
/// </summary>
public interface INexoSkillApprovalGate
{
    Task<NexoSkillApprovalStatus> RequestApprovalAsync(
        string actionDescription,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
