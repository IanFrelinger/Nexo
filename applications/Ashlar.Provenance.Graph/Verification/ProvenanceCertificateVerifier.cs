using Ashlar.Certification.Physical;
using Ashlar.Provenance.Graph.Models;

namespace Ashlar.Provenance.Graph.Verification;

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

        if (!ProvenanceClaimsCodec.TryDecode(bundle.Certificate.Extensions, out var claims, out var claimsError))
        {
            return Rejected(
                "provenance-claims-invalid",
                $"Signed provenance claims are invalid: {claimsError}",
                certHash);
        }

        claims ??= new ProvenanceClaims();
        if (!OverlayMatchesSignedClaims(bundle, claims))
        {
            return Rejected(
                "unsigned-provenance-overlay",
                "Bundle graph metadata does not match the Ed25519-signed provenance claims.",
                certHash);
        }

        var record = new VerifiedProvenanceRecord
        {
            ArtifactId = bundle.Certificate.AssetHash,
            ArtifactKind = claims.ArtifactKind,
            CertificateHash = certHash,
            IssuedAt = claims.IssuedAt,
            SignerKeyId = Hashing.ProvenanceCertificateHasher.ComputeSignerKeyId(bundle.IssuerPublicKey),
            PolicyName = claims.PolicyName,
            PolicyVersion = claims.PolicyVersion,
            ProducerAgentId = claims.ProducerAgentId,
            ProducerAgentKind = claims.ProducerAgentKind,
            DependsOnArtifactIds = claims.DependsOnArtifactIds,
            PriorCertificateHash = claims.PriorCertificateHash
        };

        return new ProvenanceVerificationResult(true, certHash, record, null, null);
    }

    private static bool OverlayMatchesSignedClaims(
        ProvenanceCertificateBundle bundle,
        ProvenanceClaims claims) =>
        bundle.ArtifactKind == claims.ArtifactKind
        && string.Equals(bundle.PolicyName, claims.PolicyName, StringComparison.Ordinal)
        && string.Equals(bundle.PolicyVersion, claims.PolicyVersion, StringComparison.Ordinal)
        && string.Equals(bundle.ProducerAgentId, claims.ProducerAgentId, StringComparison.Ordinal)
        && bundle.ProducerAgentKind == claims.ProducerAgentKind
        && bundle.DependsOnArtifactIds.SequenceEqual(claims.DependsOnArtifactIds, StringComparer.Ordinal)
        && string.Equals(bundle.PriorCertificateHash, claims.PriorCertificateHash, StringComparison.Ordinal);

    private static ProvenanceVerificationResult Rejected(string code, string reason, string certHash) =>
        new(false, certHash, null, code, reason);
}

/// <summary>Outcome of pre-projection certificate verification.</summary>
public sealed record ProvenanceVerificationResult(
    bool Trusted,
    string CertificateHash,
    VerifiedProvenanceRecord? Record,
    string? FailureCode,
    string? Reason);
