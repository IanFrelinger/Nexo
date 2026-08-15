using System.Globalization;
using Nexo.Certification.Physical.Resolution;
using Nexo.Certification.Physical.Tagging;

namespace Nexo.Spatial.Runtime.Ports;

/// <summary>
/// Adapts <see cref="PhysicalAtomTagVerifyOrchestrator"/> to the spatial resolver port.
/// </summary>
public sealed class TagVerifyResolverAdapter : IPhysicalAtomResolver
{
    private readonly IAssetResolutionStore _store;
    private readonly byte[] _issuerPublicKey;

    public TagVerifyResolverAdapter(IAssetResolutionStore store, byte[] issuerPublicKey)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        if (issuerPublicKey is null || issuerPublicKey.Length == 0)
            throw new ArgumentException("Issuer public key is required.", nameof(issuerPublicKey));

        _issuerPublicKey = issuerPublicKey;
    }

    /// <summary>Verifies and resolves a physical atom from a QR marker payload.</summary>
    public PhysicalAtomResolveResult Resolve(string markerPayload)
    {
        try
        {
            var trust = PhysicalAtomTagVerifyOrchestrator.VerifyQr(
                markerPayload,
                _store,
                _issuerPublicKey);

            if (!trust.Trusted)
                return NotFound();

            if (!PhysicalAtomQrTagCodec.TryDecode(
                    markerPayload,
                    out var reference,
                    out _,
                    out _)
                || reference is null)
            {
                return NotFound();
            }

            return new PhysicalAtomResolveResult(true, MapIdentity(reference));
        }
        catch
        {
            return NotFound();
        }
    }

    private static ResolvedAtomIdentity MapIdentity(PhysicalAtomTagReference reference) =>
        new(
            reference.AtomId.ToString("D", CultureInfo.InvariantCulture),
            Convert.ToHexString(reference.AssetHash).ToLowerInvariant(),
            reference.AssetVersion);

    private static PhysicalAtomResolveResult NotFound() => new(false, null);
}
