using System.Globalization;
using System.Text;

namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Stable cache identity for raw tile bytes on disk (host-controlled layout).
/// </summary>
public sealed class MapTileCacheKey
{
    private MapTileCacheKey(string aestheticId, string providerId, int z, int x, int y)
    {
        AestheticId = aestheticId;
        ProviderId = providerId;
        Z = z;
        X = x;
        Y = y;
    }

    public string AestheticId { get; }
    public string ProviderId { get; }
    public int Z { get; }
    public int X { get; }
    public int Y { get; }

    /// <summary>Creates a key with path-safe aesthetic and provider segments.</summary>
    public static MapTileCacheKey Create(string aestheticId, string providerId, int z, int x, int y)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aestheticId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        if (z is < 0 or > 22)
            throw new ArgumentOutOfRangeException(nameof(z));

        return new MapTileCacheKey(SanitizeSegment(aestheticId), SanitizeSegment(providerId), z, x, y);
    }

    /// <summary>Relative path using forward slashes.</summary>
    public string RelativePath =>
        $"{AestheticId}/{ProviderId}/{Z.ToString(CultureInfo.InvariantCulture)}/{X.ToString(CultureInfo.InvariantCulture)}/{Y.ToString(CultureInfo.InvariantCulture)}.bin";

    private static string SanitizeSegment(string value)
    {
        var sb = new StringBuilder();
        foreach (var c in value.Trim())
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '.' or '_')
                sb.Append(c);
            else
                sb.Append('_');
        }

        var s = sb.ToString();
        if (s.Length > 64)
            s = s[..64];
        return string.IsNullOrEmpty(s) ? "unknown" : s;
    }
}
