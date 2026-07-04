namespace Nexo.BackgroundAgents.Configuration;

/// <summary>
/// Gate for SemiActive mode: approves or rejects extender execution before it runs.
/// When mode is SemiActive, the registry calls this before running the self-extend cycle.
/// If no approval is granted, execution is skipped (observe-only for that cycle).
/// </summary>
public interface IApprovalGate
{
    /// <summary>
    /// Requests approval for an action. Blocks until approved, denied, or timed out.
    /// </summary>
    /// <param name="actionDescription">Description of the action (e.g. "Extender agent X requests approval to run self-extend cycle").</param>
    /// <param name="timeout">Maximum time to wait for approval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Approved to proceed; Denied or TimedOut to skip.</returns>
    Task<ApprovalResult> RequestApprovalAsync(string actionDescription, TimeSpan timeout, CancellationToken cancellationToken = default);
}
