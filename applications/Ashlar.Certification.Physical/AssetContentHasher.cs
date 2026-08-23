using System.Security.Cryptography;
using System.Text;

namespace Ashlar.Certification.Physical;

/// <summary>
/// Canonical SHA-256 content hash for asset bytes bound into physical-atom certificates.
/// </summary>
public static class AssetContentHasher
{
    public static string ComputeSha256Hex(ReadOnlySpan<byte> assetBytes)
    {
        if (assetBytes.IsEmpty)
            throw new ArgumentException("Asset bytes are required.", nameof(assetBytes));

        var hash = SHA256.HashData(assetBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeSha256Hex(string canonicalSource)
    {
        if (string.IsNullOrEmpty(canonicalSource))
            throw new ArgumentException("Canonical source is required.", nameof(canonicalSource));

        return ComputeSha256Hex(Encoding.UTF8.GetBytes(canonicalSource));
    }
}
