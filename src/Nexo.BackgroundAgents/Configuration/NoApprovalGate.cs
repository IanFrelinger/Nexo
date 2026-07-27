using Nexo.Core.Application.Trust.Ports;

namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// Default approval gate that never approves. Used when no IApprovalGate is registered.
/// </summary>
public sealed class NoApprovalGate : IApprovalGate
{
    /// <inheritdoc />
    public Task<ApprovalResult> RequestApprovalAsync(string actionDescription, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Task.FromResult(ApprovalResult.Denied);
}
