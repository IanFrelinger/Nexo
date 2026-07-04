using Nexo.Certification.Contracts;

namespace Nexo.Certification.State;

/// <summary>
/// Tier-2 seam: re-applies a certified behavior to confirm a transition's
/// <see cref="CertifiedTransition.ResultingStateHash"/> deterministically.
/// Tier-1 structural verification does not catch swapped valid certs or out-of-band state tampering.
/// </summary>
public interface ITransitionReplayer
{
    TransitionReplayResult Replay(
        string priorStateHash,
        string action,
        CertificationRecordData behaviorCert,
        string brickSource);
}
