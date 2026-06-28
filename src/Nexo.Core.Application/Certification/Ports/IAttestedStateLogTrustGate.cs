using Nexo.Certification.State;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Application trust boundary for attested state logs (delegates to <see cref="StateLogVerifier"/>).
/// </summary>
public interface IAttestedStateLogTrustGate
{
    StateLogTrustResult Verify(
        AttestedStateLog log,
        StateSchema schema,
        string? hmacKey = null,
        ITransitionReplayer? replayer = null,
        ILiveStateHashReader? liveStateReader = null);

    /// <summary>
    /// Tier-2 brick replay plus optional Tier-3 live head binding in one call.
    /// </summary>
    StateLogTrustResult VerifyFullTrust(
        AttestedStateLog log,
        StateSchema schema,
        string? hmacKey = null,
        string? liveStateHash = null);
}
