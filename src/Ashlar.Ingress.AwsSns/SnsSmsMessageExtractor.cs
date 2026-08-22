using System.Text.Json;

namespace Ashlar.Ingress.AwsSns;

/// <summary>
/// Extracts SMS sender and body text from the SNS <c>Message</c> field (plain text or JSON from End User Messaging).
/// </summary>
public static class SnsSmsMessageExtractor
{
    /// <summary>
    /// Parses the outer SNS <c>Message</c> string. Supports plain text and JSON objects that include
    /// <c>originationNumber</c> / <c>messageBody</c> (common for SMS-to-SNS payloads).
    /// </summary>
    public static bool TryExtract(string snsMessageField, out string? originatingAddress, out string bodyText)
    {
        originatingAddress = null;
        bodyText = snsMessageField?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(bodyText))
            return false;

        if (!bodyText.StartsWith("{", StringComparison.Ordinal))
            return true;

        try
        {
            using var doc = JsonDocument.Parse(bodyText);
            var root = doc.RootElement;
            if (root.TryGetProperty("messageBody", out var mb) && mb.ValueKind == JsonValueKind.String)
                bodyText = mb.GetString() ?? bodyText;
            if (root.TryGetProperty("originationNumber", out var origin) && origin.ValueKind == JsonValueKind.String)
                originatingAddress = origin.GetString();
        }
        catch (JsonException)
        {
            // Treat invalid JSON as opaque body text
        }

        return !string.IsNullOrWhiteSpace(bodyText);
    }
}
