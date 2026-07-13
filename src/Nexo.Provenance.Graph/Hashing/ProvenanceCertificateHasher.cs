using System.Security.Cryptography;
using Nexo.Certification.Physical;

namespace Nexo.Provenance.Graph.Hashing;

/// <summary>Canonical hashing for provenance graph certificate and key identifiers.</summary>
public static class ProvenanceCertificateHasher
{
    /// <summary>SHA-256 hex (lowercase) of the Ed25519 signing payload — certificate node id.</summary>
    public static string ComputeCertificateHash(PhysicalAtomCertificate certificate)
    {
        var payload = PhysicalAtomCertificateSigning.BuildSigningPayload(certificate);
        var hash = SHA256.HashData(payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Stable signer key id derived from issuer public key bytes.</summary>
    public static string ComputeSignerKeyId(ReadOnlySpan<byte> issuerPublicKey)
    {
        if (issuerPublicKey.IsEmpty)
            throw new ArgumentException("Issuer public key is required.", nameof(issuerPublicKey));

        var hash = SHA256.HashData(issuerPublicKey);
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }

    /// <summary>Policy version node id from name and version.</summary>
    public static string ComputePolicyVersionId(string policyName, string policyVersion) =>
        $"{policyName}@{policyVersion}";
}
