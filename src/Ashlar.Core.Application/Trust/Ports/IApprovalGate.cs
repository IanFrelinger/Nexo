namespace Ashlar.Core.Application.Trust.Ports;

/// <summary>Result of a human-approval request.</summary>
public enum ApprovalResult
{
    /// <summary>Reviewer approved the action.</summary>
    Approved = 0,

    /// <summary>Reviewer denied the action.</summary>
    Denied = 1,

    /// <summary>No decision within the allotted timeout.</summary>
    TimedOut = 2
}

/// <summary>
/// Human-review port. Used when generative provenance requires review
/// (<c>RequiresHumanReview</c>) before a side effect (install/deploy) proceeds.
/// </summary>
public interface IApprovalGate
{
    /// <summary>
    /// Requests approval for an action. Blocks until approved, denied, or timed out.
    /// </summary>
    Task<ApprovalResult> RequestApprovalAsync(
        string actionDescription,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
