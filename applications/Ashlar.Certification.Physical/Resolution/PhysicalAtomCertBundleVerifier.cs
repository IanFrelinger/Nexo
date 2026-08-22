namespace Ashlar.Certification.Physical.Resolution;

/// <summary>
/// Verifies a self-contained certified bundle (certificate + asset bytes + issuer public key).
/// </summary>
public static class PhysicalAtomCertBundleVerifier
{
    public static PhysicalAtomTrustResult Verify(
        PhysicalAtomCertBundle bundle,
        ReadOnlySpan<byte> issuerPublicKey)
    {
        if (bundle is null)
            return Refused("bundle-missing", "Physical atom cert bundle is required.");

        if (bundle.AssetBytes is null || bundle.AssetBytes.Length == 0)
            return Refused("bundle-asset-missing", "Bundle asset bytes are required.");

        if (issuerPublicKey.IsEmpty)
            return Refused("issuer-public-key-missing", "Issuer public key is required.");

        if (!string.Equals(
                AssetContentHasher.ComputeSha256Hex(bundle.AssetBytes),
                bundle.Certificate.AssetHash,
                StringComparison.Ordinal))
        {
            return Refused(
                "bundle-asset-hash-mismatch",
                "Bundle asset bytes do not match certificate asset_hash.");
        }

        if (!bundle.IssuerPublicKey.AsSpan().SequenceEqual(issuerPublicKey))
        {
            return Refused(
                "bundle-issuer-key-mismatch",
                "Bundle issuer public key does not match the expected issuer key.");
        }

        return PhysicalAtomCertificateVerifier.Verify(
            bundle.Certificate,
            bundle.AssetBytes,
            issuerPublicKey);
    }

    private static PhysicalAtomTrustResult Refused(string code, string reason) =>
        new(false, code, reason);
}
