namespace Nexo.Certification.Physical;

/// <summary>
/// Standalone physical-atom certificate verifier (StateLogVerifier lineage: structural trust checks).
/// </summary>
public static class PhysicalAtomCertificateVerifier
{
    public static PhysicalAtomTrustResult Verify(
        PhysicalAtomCertificate certificate,
        ReadOnlySpan<byte> assetBytes,
        ReadOnlySpan<byte> issuerPublicKey)
    {
        if (certificate is null)
            return Refused("certificate-missing", "Physical atom certificate is required.");

        if (assetBytes.IsEmpty)
            return Refused("asset-bytes-missing", "Asset bytes are required for hash consistency verification.");

        if (issuerPublicKey.IsEmpty)
            return Refused("issuer-public-key-missing", "Issuer public key is required.");

        if (string.IsNullOrWhiteSpace(certificate.IssuerSignature))
            return Refused("signature-missing", "issuer_signature is required.");

        if (!PhysicalAtomCertificateValidationPolicy.ValidateBindingScopeConsistency(
                certificate,
                out var policyCode,
                out var policyReason))
        {
            return Refused(policyCode!, policyReason!);
        }

        if (!PhysicalAtomCertificateSigning.VerifySignature(certificate, issuerPublicKey))
            return Refused("signature-invalid", "issuer_signature is invalid or forged.");

        var actualHash = AssetContentHasher.ComputeSha256Hex(assetBytes);
        if (!string.Equals(actualHash, certificate.AssetHash, StringComparison.Ordinal))
        {
            return Refused(
                "asset-hash-mismatch",
                $"Provided asset hash does not match certificate asset_hash (expected {certificate.AssetHash}, got {actualHash}).");
        }

        return new PhysicalAtomTrustResult(true, null, null);
    }

    private static PhysicalAtomTrustResult Refused(string code, string reason) =>
        new(false, code, reason);
}

public sealed record PhysicalAtomTrustResult(bool Trusted, string? FailureCode, string? Reason);
