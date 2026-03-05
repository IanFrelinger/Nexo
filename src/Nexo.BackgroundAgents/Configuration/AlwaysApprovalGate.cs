namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// Approval gate that always approves. Used for testing SemiActive mode
/// or when approval is granted by other means (e.g. env var, config).
/// </summary>
public sealed class AlwaysApprovalGate : IApprovalGate
{
    public Task<ApprovalResult> RequestApprovalAsync(string actionDescription, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Task.FromResult(ApprovalResult.Approved);
}
