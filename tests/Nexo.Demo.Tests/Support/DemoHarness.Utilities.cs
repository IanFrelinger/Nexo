using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Nexo.Demo.Tests.Support;

/// <summary>
/// Utilities functionality for demo harness.
/// </summary>
public static partial class DemoHarness
{
    /// <summary>
    /// Calculates SHA-256 hash of a file
    /// </summary>
    public static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Calculates Jaccard similarity between two tokenized strings
    /// </summary>
    public static double JaccardTokens(string a, string b)
    {
        static string[] Tokenize(string s) => s.ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ';', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '/', ':', '\\' },
                   StringSplitOptions.RemoveEmptyEntries);

        var tokensA = Tokenize(a).ToHashSet();
        var tokensB = Tokenize(b).ToHashSet();
        var intersection = tokensA.Intersect(tokensB).Count();
        var union = tokensA.Union(tokensB).Count();
        return union == 0 ? 1.0 : (double)intersection / union;
    }

    /// <summary>
    /// Normalizes text by removing non-deterministic elements
    /// </summary>
    public static string NormalizeText(string text)
    {
        // Remove timestamps, IDs, and other non-deterministic elements
        return Regex.Replace(text, @"\b(id|ts|timestamp|time)\s*[:=]\s*[\w\-\.:]+", " ", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Gets the current network attempt count
    /// </summary>
    public static int GetNetworkAttempts() => _networkAttempts;

    /// <summary>
    /// Increments the network attempt counter
    /// </summary>
    public static void IncrementNetworkAttempts() => Interlocked.Increment(ref _networkAttempts);
}
