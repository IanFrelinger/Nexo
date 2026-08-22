using System.Security.Cryptography;
using System.Text;

namespace Ashlar.Infrastructure.Caching;

/// <summary>
/// Shared utility for generating cache keys from content.
/// Used by CachedValidationServiceAdapter and CachedAnalysisServiceAdapter.
/// </summary>
public static class CacheKeyGenerator
{
    /// <summary>
    /// Computes a SHA256 hash of the content, returned as Base64.
    /// </summary>
    public static Task<string> ComputeHashAsync(string content, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }, cancellationToken);
    }
}
