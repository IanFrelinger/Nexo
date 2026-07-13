using System.Security.Cryptography;

namespace Nexo.Core.Application.Skills;

/// <summary>Content hashing helpers for skill cache invalidation and audit.</summary>
public static class SkillContentHasher
{
    /// <summary>Computes SHA-256 hex digest of a file's contents.</summary>
    public static string ComputeFileHash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
