using System.Text.RegularExpressions;

namespace Nexo.API.Forge;

/// <summary>
/// Normalizes client-supplied Forge tenant ids for safe use as LiteDB keys and dictionary keys.
/// </summary>
public static partial class ForgeTenantNormalizer
{
    public const string DefaultTenantId = "default";
    private const int MaxLength = 64;

    [GeneratedRegex(@"[^a-zA-Z0-9\-_]", RegexOptions.Compiled)]
    private static partial Regex InvalidChars();

    /// <summary>
    /// Returns a non-empty tenant id safe for storage keys, or <see cref="DefaultTenantId"/>.
    /// </summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DefaultTenantId;

        var trimmed = raw.Trim();
        if (trimmed.Length > MaxLength)
            trimmed = trimmed[..MaxLength];

        trimmed = InvalidChars().Replace(trimmed, "-");
        return string.IsNullOrWhiteSpace(trimmed) ? DefaultTenantId : trimmed;
    }
}
