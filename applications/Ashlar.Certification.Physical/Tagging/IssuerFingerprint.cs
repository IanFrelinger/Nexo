using System.Security.Cryptography;
using System.Text;

namespace Ashlar.Certification.Physical.Tagging;

/// <summary>
/// Computes issuer public-key fingerprints for tag disambiguation.
/// </summary>
public static class IssuerFingerprint
{
    public static byte[] Compute(ReadOnlySpan<byte> issuerPublicKey)
    {
        if (issuerPublicKey.IsEmpty)
            throw new ArgumentException("Issuer public key is required.", nameof(issuerPublicKey));

        var hash = SHA256.HashData(issuerPublicKey);
        return hash.AsSpan(0, PhysicalAtomTagReference.IssuerFingerprintLength).ToArray();
    }
}
