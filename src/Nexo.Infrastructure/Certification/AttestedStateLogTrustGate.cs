using Nexo.Certification.State;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Fail-closed trust gate for attested state logs using an injected behavior catalog.
/// </summary>
public sealed class AttestedStateLogTrustGate : IAttestedStateLogTrustGate
{
    private readonly ICertificateResolver _resolver;
    private readonly IBrickTransitionReplayerFactory _replayerFactory;

    public AttestedStateLogTrustGate(
        ICertifiedBehaviorCatalog catalog,
        IBrickTransitionReplayerFactory replayerFactory)
    {
        if (catalog is null)
            throw new ArgumentNullException(nameof(catalog));

        _resolver = new CertifiedBehaviorCertificateResolver(catalog);
        _replayerFactory = replayerFactory ?? throw new ArgumentNullException(nameof(replayerFactory));
    }

    public StateLogTrustResult Verify(
        AttestedStateLog log,
        StateSchema schema,
        string? hmacKey = null,
        ITransitionReplayer? replayer = null,
        ILiveStateHashReader? liveStateReader = null) =>
        StateLogVerifier.Verify(log, schema, _resolver, hmacKey, replayer, liveStateReader);

    public StateLogTrustResult VerifyFullTrust(
        AttestedStateLog log,
        StateSchema schema,
        string? hmacKey = null,
        string? liveStateHash = null)
    {
        var replayer = _replayerFactory.Create(schema);
        ILiveStateHashReader? liveReader = liveStateHash is null
            ? null
            : new FixedLiveStateHashReader(liveStateHash);

        return Verify(log, schema, hmacKey, replayer, liveReader);
    }
}
