using System.Security.Cryptography;
using Ashlar.Certification.Physical;

namespace Ashlar.Provenance.Graph.Hashing;

/// <summary>Canonical hashing for provenance graph certificate and key identifiers.</summary>
public static class ProvenanceCertificateHasher
{
    /// <summary>
    /// SHA-256 hex (lowercase) of the complete certificate content:
    /// canonical signed payload plus the encoded Ed25519 signature.
    /// </summary>
    public static string ComputeCertificateHash(PhysicalAtomCertificate certificate)
    {
        var payload = PhysicalAtomCertificateSigning.BuildSigningPayload(certificate);
        var signature = System.Text.Encoding.UTF8.GetBytes(certificate.IssuerSignature ?? string.Empty);
        var content = new byte[payload.Length + 1 + signature.Length];
        payload.CopyTo(content, 0);
        content[payload.Length] = (byte)'\n';
        signature.CopyTo(content, payload.Length + 1);
        var hash = SHA256.HashData(content);
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
