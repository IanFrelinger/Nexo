using Ashlar.Certification.Physical.Tagging;

namespace Ashlar.Certification.Physical.Resolution;

/// <summary>
/// Phase 3 orchestrator: decode physical tag → resolve hosted cert/asset → Phase 0 verify.
/// </summary>
public static class PhysicalAtomTagVerifyOrchestrator
{
    public static PhysicalAtomTrustResult VerifyQr(
        string qrPayload,
        IAssetResolutionStore store,
        ReadOnlySpan<byte> issuerPublicKey)
    {
        if (!PhysicalAtomQrTagCodec.TryDecode(
                qrPayload,
                out var reference,
                out var failureCode,
                out var reason))
        {
            return Refused(failureCode!, reason!);
        }

        return VerifyReference(reference!, store, issuerPublicKey);
    }

    public static PhysicalAtomTrustResult VerifyReference(
        PhysicalAtomTagReference reference,
        IAssetResolutionStore store,
        ReadOnlySpan<byte> issuerPublicKey)
    {
        if (reference is null)
            return Refused("tag-reference-missing", "Tag reference is required.");

        if (store is null)
            return Refused("store-missing", "Asset resolution store is required.");

        if (issuerPublicKey.IsEmpty)
            return Refused("issuer-public-key-missing", "Issuer public key is required.");

        var expectedFingerprint = IssuerFingerprint.Compute(issuerPublicKey);
        if (!reference.IssuerFingerprint.AsSpan().SequenceEqual(expectedFingerprint))
        {
            return Refused(
                "tag-issuer-fingerprint-mismatch",
                "Tag issuer fingerprint does not match the expected issuer public key.");
        }

        if (!store.TryResolveCert(reference.AtomId, out var certificate) || certificate is null)
        {
            return Refused(
                "atom-unresolved",
                $"No registered certificate for atom_id '{reference.AtomId}'.");
        }

        if (!TagMatchesCertificate(reference, certificate))
        {
            return Refused(
                "tag-reference-mismatch",
                "Tag reference fields do not match the registered certificate.");
        }

        return PhysicalAtomResolutionVerifier.Verify(certificate, store, issuerPublicKey);
    }

    private static bool TagMatchesCertificate(
        PhysicalAtomTagReference reference,
        PhysicalAtomCertificate certificate)
    {
        byte[] certHashBytes;
        try
        {
            certHashBytes = Convert.FromHexString(certificate.AssetHash);
        }
        catch (FormatException)
        {
            return false;
        }

        return reference.AtomId == certificate.AtomId
            && reference.AssetHash.AsSpan().SequenceEqual(certHashBytes)
            && string.Equals(reference.AssetVersion, certificate.AssetVersion, StringComparison.Ordinal);
    }

    private static PhysicalAtomTrustResult Refused(string code, string reason) =>
        new(false, code, reason);
}
