using Ashlar.Certification.Contracts;

namespace Ashlar.Certification.State;

/// <summary>
/// Outcome of replaying one certified transition.
/// </summary>
public sealed class TransitionReplayResult
{
    public TransitionReplayResult(bool matches, string? computedStateHash, string? failureCode, string? reason)
    {
        Matches = matches;
        ComputedStateHash = computedStateHash;
        FailureCode = failureCode;
        Reason = reason;
    }

    public bool Matches { get; }
    public string? ComputedStateHash { get; }
    public string? FailureCode { get; }
    public string? Reason { get; }
}
