using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;

namespace Nexo.Infrastructure.Skills;

/// <summary>
/// Human-in-the-loop gate: signals the resolver to wait on
/// <see cref="INexoSkillApprovalStore.WaitForResolutionAsync"/>.
/// Operators resolve via CLI/API (<c>TryResolve</c>).
/// </summary>
public sealed class HumanInTheLoopSkillApprovalGate : INexoSkillApprovalGate
{
    /// <inheritdoc />
    public Task<NexoSkillApprovalStatus> RequestApprovalAsync(
        string actionDescription,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        => Task.FromResult(NexoSkillApprovalStatus.Pending);
}
