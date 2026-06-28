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
        ITransitionReplayer? replayer = null);
}
