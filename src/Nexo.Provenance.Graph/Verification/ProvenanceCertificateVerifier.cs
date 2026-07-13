using Nexo.Certification.Physical;
using Nexo.Provenance.Graph.Models;

namespace Nexo.Provenance.Graph.Verification;

/// <summary>Ed25519 signature and content-bound hash verification before graph projection.</summary>
public static class ProvenanceCertificateVerifier
{
    public static ProvenanceVerificationResult Verify(ProvenanceCertificateBundle bundle)
    {
        if (bundle is null)
            return Rejected("bundle-missing", "Certificate bundle is required.", string.Empty);

        var certHash = Hashing.ProvenanceCertificateHasher.ComputeCertificateHash(bundle.Certificate);

        if (bundle.ArtifactContent is null || bundle.ArtifactContent.Length == 0)
            return Rejected("artifact-content-missing", "Artifact content is required.", certHash);

        if (bundle.IssuerPublicKey is null || bundle.IssuerPublicKey.Length == 0)
            return Rejected("issuer-public-key-missing", "Issuer public key is required.", certHash);

        var trust = PhysicalAtomCertificateVerifier.Verify(
            bundle.Certificate,
            bundle.ArtifactContent,
            bundle.IssuerPublicKey);

        if (!trust.Trusted)
            return Rejected(trust.FailureCode ?? "verification-failed", trust.Reason ?? "Verification failed.", certHash);

        var actualArtifactId = AssetContentHasher.ComputeSha256Hex(bundle.ArtifactContent);
        if (!string.Equals(actualArtifactId, bundle.ArtifactId, StringComparison.Ordinal))
        {
            return Rejected(
                "artifact-id-mismatch",
                $"Bundle artifact id does not match content hash (expected {bundle.ArtifactId}, got {actualArtifactId}).",
                certHash);
        }

        if (!string.Equals(bundle.Certificate.AssetHash, bundle.ArtifactId, StringComparison.Ordinal))
        {
            return Rejected(
                "content-hash-mismatch",
                $"Certificate asset_hash does not match bound artifact (expected {bundle.ArtifactId}, cert has {bundle.Certificate.AssetHash}).",
                certHash);
        }

        return new ProvenanceVerificationResult(true, certHash, null, null);
    }

    private static ProvenanceVerificationResult Rejected(string code, string reason, string certHash) =>
        new(false, certHash, code, reason);
}

/// <summary>Outcome of pre-projection certificate verification.</summary>
public sealed record ProvenanceVerificationResult(
    bool Trusted,
    string CertificateHash,
    string? FailureCode,
    string? Reason);
