using System.Text;
using System.Text.Json;

namespace Ashlar.Abstractions.Barriers.Identity;

/// <summary>
/// Lightweight JWT payload parser for barrier identity resolution.
/// Decodes the middle segment of a JWT without validating signatures or expiry;
/// resolvers use the extracted claims as hints only.
/// </summary>
public static class JwtClaimParser
{
    /// <summary>
    /// Parses claim name/value pairs from a JWT payload segment.
    /// Returns an empty dictionary when the token is null, malformed, or cannot be decoded.
    /// Never throws; callers treat an empty result as "no JWT claims available".
    /// </summary>
    /// <param name="rawJwt">Full JWT string (<c>header.payload.signature</c>) or raw JSON payload.</param>
    public static IReadOnlyDictionary<string, string> ParseClaims(string? rawJwt)
    {
        var token = rawJwt?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            return EmptyClaims;
        var nonNullToken = token!;

        try
        {
            var parts = nonNullToken.Split('.');
            if (parts.Length < 2)
                return EmptyClaims;

            var payload = parts[1];
            var json = payload.Length > 0 && payload[0] == '{'
                ? payload
                : DecodeBase64UrlAsUtf8(payload);

            if (string.IsNullOrWhiteSpace(json))
                return EmptyClaims;

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return EmptyClaims;

            var claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                claims[property.Name] = ClaimValueToString(property.Value);
            }

            return claims;
        }
        catch (FormatException)
        {
            return EmptyClaims;
        }
        catch (JsonException)
        {
            return EmptyClaims;
        }
        catch (ArgumentException)
        {
            return EmptyClaims;
        }
    }

    private static string DecodeBase64UrlAsUtf8(string base64Url)
    {
        var normalized = base64Url.Replace('-', '+').Replace('_', '/');
        switch (normalized.Length % 4)
        {
            case 2:
                normalized += "==";
                break;
            case 3:
                normalized += "=";
                break;
            case 0:
                break;
            default:
                return string.Empty;
        }

        var bytes = Convert.FromBase64String(normalized);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string ClaimValueToString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => value.GetRawText()
        };
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyClaims
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
