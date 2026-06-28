using Nexo.Certification.State;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Fail-closed trust gate for attested state logs using an injected behavior catalog.
/// </summary>
public sealed class AttestedStateLogTrustGate : IAttestedStateLogTrustGate
{
    private readonly ICertificateResolver _resolver;

    public AttestedStateLogTrustGate(ICertifiedBehaviorCatalog catalog)
    {
        if (catalog is null)
            throw new ArgumentNullException(nameof(catalog));

        _resolver = new CertifiedBehaviorCertificateResolver(catalog);
    }

    public StateLogTrustResult Verify(
        AttestedStateLog log,
        StateSchema schema,
        string? hmacKey = null,
        ITransitionReplayer? replayer = null) =>
        StateLogVerifier.Verify(log, schema, _resolver, hmacKey, replayer);
}
