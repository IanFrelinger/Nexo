namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// Approval gate that always denies. Used for testing SemiActive mode denial path.
/// </summary>
public sealed class DenyApprovalGate : IApprovalGate
{
    public Task<ApprovalResult> RequestApprovalAsync(string actionDescription, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Task.FromResult(ApprovalResult.Denied);
}
