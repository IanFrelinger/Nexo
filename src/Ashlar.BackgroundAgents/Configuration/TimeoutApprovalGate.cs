using Ashlar.Core.Application.Trust.Ports;

namespace Ashlar.BackgroundAgents.Configuration;

/// <summary>
/// Approval gate that always times out. Used in tests.
/// </summary>
public sealed class TimeoutApprovalGate : IApprovalGate
{
    /// <inheritdoc />
    public Task<ApprovalResult> RequestApprovalAsync(string actionDescription, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        Task.FromResult(ApprovalResult.TimedOut);
}
