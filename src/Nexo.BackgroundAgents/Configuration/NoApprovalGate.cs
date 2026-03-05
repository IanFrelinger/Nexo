namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// Default approval gate that never approves. Used when no IApprovalGate is registered.
/// SemiActive mode will skip execution when this gate is used.
/// </summary>
public sealed class NoApprovalGate : IApprovalGate
{
    public Task<ApprovalResult> RequestApprovalAsync(string actionDescription, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Task.FromResult(ApprovalResult.Denied);
}
