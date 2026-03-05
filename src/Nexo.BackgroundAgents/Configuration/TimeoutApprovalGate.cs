namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// Approval gate that always returns TimedOut. Used for testing SemiActive mode timeout path.
/// </summary>
public sealed class TimeoutApprovalGate : IApprovalGate
{
    public Task<ApprovalResult> RequestApprovalAsync(string actionDescription, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Task.FromResult(ApprovalResult.TimedOut);
}
