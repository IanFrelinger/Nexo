using System.Globalization;
using System.Text;

namespace Nexo.Commercial.GameDomain.Mapping;
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

    /// <summary>aesthetic id value.</summary>
    public string AestheticId { get; }
    /// <summary>provider id value.</summary>
    public string ProviderId { get; }
    /// <summary>Z value.</summary>
    public int Z { get; }
    /// <summary>X value.</summary>
    public int X { get; }
    /// <summary>Y value.</summary>
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
