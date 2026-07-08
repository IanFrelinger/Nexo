using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexo.VisualVerification;

/// <summary>Canonical JSON serialization for content-addressed visual verification records.</summary>
public static class CanonicalJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}

/// <summary>SHA-256 content hashes for visual verification artifacts.</summary>
public static class ContentSha256
{
    public static string ComputeHex(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
            throw new ArgumentException("Bytes are required.", nameof(bytes));

        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    public static string ComputeHex(string canonicalSource)
    {
        if (string.IsNullOrEmpty(canonicalSource))
            throw new ArgumentException("Canonical source is required.", nameof(canonicalSource));

        return ComputeHex(Encoding.UTF8.GetBytes(canonicalSource));
    }
}
