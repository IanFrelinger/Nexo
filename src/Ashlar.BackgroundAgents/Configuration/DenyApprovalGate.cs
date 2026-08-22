using Ashlar.Core.Application.Trust.Ports;

namespace Ashlar.BackgroundAgents.Configuration;

/// <summary>
/// Approval gate that always denies. Used for ObserveOnly-style SemiActive.
/// </summary>
public sealed class DenyApprovalGate : IApprovalGate
{
    /// <inheritdoc />
    public Task<ApprovalResult> RequestApprovalAsync(string actionDescription, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Task.FromResult(ApprovalResult.Denied);
}
