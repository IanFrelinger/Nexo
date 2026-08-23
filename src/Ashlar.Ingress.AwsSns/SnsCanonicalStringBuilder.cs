using System.Text;
using System.Text.Json;

namespace Ashlar.Ingress.AwsSns;

/// <summary>
/// Builds the canonical string Amazon SNS signs for HTTP(S) notifications and subscription handshakes.
/// See https://docs.aws.amazon.com/sns/latest/dg/sns-verify-signature-of-message-verify-message-signature.html
/// </summary>
public static class SnsCanonicalStringBuilder
{
    /// <summary>Attempts to build the string-to-sign for the given SNS JSON body.</summary>
    public static bool TryBuild(JsonElement root, out string stringToSign, out string? error)
    {
        stringToSign = string.Empty;
        error = null;

        if (!root.TryGetProperty("Type", out var typeProp) || typeProp.ValueKind != JsonValueKind.String)
        {
            error = "Missing Type.";
            return false;
        }

        var type = typeProp.GetString();
        var keys = type switch
        {
            "Notification" => BuildNotificationKeys(root),
            "SubscriptionConfirmation" or "UnsubscribeConfirmation" => BuildConfirmationKeys(),
            _ => null
        };

        if (keys is null)
        {
            error = $"Unsupported SNS Type '{type}'.";
            return false;
        }

        var segments = new List<string>(keys.Count);
        foreach (var key in keys)
        {
            if (!root.TryGetProperty(key, out var prop))
            {
                error = $"Missing required property '{key}'.";
                return false;
            }

            if (prop.ValueKind != JsonValueKind.String)
            {
                error = $"Property '{key}' must be a string.";
                return false;
            }

            var value = prop.GetString() ?? string.Empty;
            segments.Add($"{key}\n{value}");
        }

        stringToSign = string.Join("\n", segments);
        return true;
    }

    private static List<string> BuildNotificationKeys(JsonElement root)
    {
        var keys = new List<string> { "Message", "MessageId", "Timestamp", "TopicArn", "Type" };
        if (root.TryGetProperty("Subject", out var subject) && subject.ValueKind == JsonValueKind.String)
            keys.Add("Subject");

        keys.Sort(StringComparer.Ordinal);
        return keys;
    }

    private static List<string> BuildConfirmationKeys()
    {
        var keys = new List<string> { "Message", "MessageId", "SubscribeURL", "Timestamp", "Token", "TopicArn", "Type" };
        keys.Sort(StringComparer.Ordinal);
        return keys;
    }
}
