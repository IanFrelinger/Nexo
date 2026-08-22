namespace Ashlar.Certification.Physical.Resolution;

/// <summary>
/// Verifies a certificate by resolving hosted asset bytes from a store, then delegating to the Phase 0 verifier.
/// </summary>
public static class PhysicalAtomResolutionVerifier
{
    public static PhysicalAtomTrustResult Verify(
        PhysicalAtomCertificate certificate,
        IAssetResolutionStore store,
        ReadOnlySpan<byte> issuerPublicKey)
    {
        if (certificate is null)
            return Refused("certificate-missing", "Physical atom certificate is required.");

        if (store is null)
            return Refused("store-missing", "Asset resolution store is required.");

        if (issuerPublicKey.IsEmpty)
            return Refused("issuer-public-key-missing", "Issuer public key is required.");

        if (!store.TryResolveCert(certificate.AtomId, out var registered) || registered is null)
            return Refused("atom-unresolved", $"No registered certificate for atom_id '{certificate.AtomId}'.");

        if (!ReferenceEquals(registered, certificate)
            && !CertificatesEquivalent(registered, certificate))
        {
            return Refused(
                "atom-cert-mismatch",
                "Provided certificate does not match the registered certificate for this atom_id.");
        }

        if (!store.TryResolveAsset(certificate.AssetHash, certificate.AssetVersion, out var asset)
            || asset is null)
        {
            return Refused(
                "asset-unresolved",
                $"No hosted asset for hash '{certificate.AssetHash}' version '{certificate.AssetVersion}'.");
        }

        return PhysicalAtomCertificateVerifier.Verify(
            certificate,
            asset.AssetBytes,
            issuerPublicKey);
    }

    private static bool CertificatesEquivalent(PhysicalAtomCertificate left, PhysicalAtomCertificate right) =>
        left.SchemaVersion == right.SchemaVersion
        && left.Maturity == right.Maturity
        && left.AtomId == right.AtomId
        && left.BindingScope == right.BindingScope
        && string.Equals(left.AssetHash, right.AssetHash, StringComparison.Ordinal)
        && string.Equals(left.AssetVersion, right.AssetVersion, StringComparison.Ordinal)
        && string.Equals(left.IssuerSignature, right.IssuerSignature, StringComparison.Ordinal);

    private static PhysicalAtomTrustResult Refused(string code, string reason) =>
        new(false, code, reason);
}
