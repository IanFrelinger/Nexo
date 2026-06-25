namespace Nexo.Certification.State;

/// <summary>
/// Trust decision for an attested state log.
/// </summary>
public sealed class StateLogTrustResult
{
    public StateLogTrustResult(bool trusted, string? failureCode, string? reason, int verifiedTransitions)
    {
        Trusted = trusted;
        FailureCode = failureCode;
        Reason = reason;
        VerifiedTransitions = verifiedTransitions;
    }

    public bool Trusted { get; }
    public string? FailureCode { get; }
    public string? Reason { get; }
    public int VerifiedTransitions { get; }
}
