using Nexo.Core.Application.Trust.Ports;

namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// Approval gate that always approves. Used for Active mode or when operator has pre-approved.
/// </summary>
public sealed class AlwaysApprovalGate : IApprovalGate
{
    /// <inheritdoc />
    public Task<ApprovalResult> RequestApprovalAsync(string actionDescription, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Task.FromResult(ApprovalResult.Approved);
}
