using System.Security.Cryptography;
using System.Text;

namespace Nexo.Certification.Contracts;

/// <summary>
/// Canonical SHA-256 content hash for brick source bound into certification records.
/// </summary>
public static class BrickContentHasher
{
    public static string ComputeSha256(string canonicalSource)
    {
        if (string.IsNullOrEmpty(canonicalSource))
            throw new ArgumentException("Canonical source is required.", nameof(canonicalSource));
        var bytes = Encoding.UTF8.GetBytes(canonicalSource);
#if NET5_0_OR_GREATER
        var hash = SHA256.HashData(bytes);
#else
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
#endif
        return Convert.ToBase64String(hash);
    }
}
