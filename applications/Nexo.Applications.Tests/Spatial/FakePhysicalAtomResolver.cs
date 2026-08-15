using Nexo.Spatial.Runtime.Ports;

namespace Nexo.Tests.Infrastructure.Tests.Spatial;

/// <summary>
/// Cert-independent fake resolver for spatial rejection tests.
/// </summary>
internal sealed class FakePhysicalAtomResolver : IPhysicalAtomResolver
{
    private readonly Dictionary<string, ResolvedAtomIdentity> _identitiesByMarker;

    public FakePhysicalAtomResolver(IEnumerable<(string MarkerPayload, ResolvedAtomIdentity Identity)> entries)
    {
        _identitiesByMarker = entries.ToDictionary(
            e => e.MarkerPayload,
            e => e.Identity,
            StringComparer.Ordinal);
    }

    public Func<string, PhysicalAtomResolveResult>? ResolveOverride { get; set; }

    public PhysicalAtomResolveResult Resolve(string markerPayload)
    {
        if (ResolveOverride is not null)
            return ResolveOverride(markerPayload);

        if (_identitiesByMarker.TryGetValue(markerPayload, out var identity))
            return new PhysicalAtomResolveResult(true, identity);

        return new PhysicalAtomResolveResult(false, null);
    }

    public void ReplaceIdentity(string markerPayload, ResolvedAtomIdentity identity) =>
        _identitiesByMarker[markerPayload] = identity;
}
