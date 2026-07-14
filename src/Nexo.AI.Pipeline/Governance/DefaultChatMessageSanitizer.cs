using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace Nexo.AI.Pipeline.Governance;

/// <summary>
/// Built-in PII/secret filter used when no host-specific sanitizer is registered.
/// Mirrors the categories covered by Nexo's <c>SensitiveContentFilter</c>.
/// </summary>
public sealed partial class DefaultChatMessageSanitizer : IChatMessageSanitizer
{
    /// <inheritdoc />
    public MessageSanitizationResult Sanitize(
        IList<ChatMessage> messages,
        string targetKey,
        SanitizeDisposition disposition,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = targetKey;

        if (disposition == SanitizeDisposition.Pass)
        {
            return MessageSanitizationResult.Allow(messages);
        }

        var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var redactionCount = 0;
        var sanitized = new List<ChatMessage>(messages.Count);

        foreach (var message in messages)
        {
            var original = message.Text ?? string.Empty;
            Detect(original, categories, out var hasSecret, out var hasPii);

            if (disposition == SanitizeDisposition.BlockOnPiiOrSecret && (hasSecret || hasPii))
            {
                return MessageSanitizationResult.Block(
                    "Outbound content contains PII or secrets; blocked per policy.",
                    categories.ToList());
            }

            if (disposition == SanitizeDisposition.BlockOnSecretRedactOnPii && hasSecret)
            {
                return MessageSanitizationResult.Block(
                    "Outbound content contains secrets; blocked per policy.",
                    categories.ToList());
            }

            if (disposition is SanitizeDisposition.Redact
                or SanitizeDisposition.BlockOnSecretRedactOnPii
                or SanitizeDisposition.BlockOnPiiOrSecret)
            {
                var filtered = Redact(original);
                if (!string.Equals(filtered, original, StringComparison.Ordinal))
                {
                    redactionCount++;
                    sanitized.Add(new ChatMessage(message.Role, filtered));
                    continue;
                }
            }

            sanitized.Add(message);
        }

        return MessageSanitizationResult.Allow(sanitized, redactionCount, categories.ToList());
    }

    private static void Detect(string text, ISet<string> categories, out bool hasSecret, out bool hasPii)
    {
        hasSecret = false;
        hasPii = false;

        if (ApiKeyRegex().IsMatch(text))
        {
            hasSecret = true;
            categories.Add("api-key");
        }

        if (EmailRegex().IsMatch(text))
        {
            hasPii = true;
            categories.Add("email");
        }

        if (PhoneRegex().IsMatch(text))
        {
            hasPii = true;
            categories.Add("phone");
        }

        if (SsnRegex().IsMatch(text))
        {
            hasPii = true;
            categories.Add("ssn");
        }

        if (CreditCardRegex().IsMatch(text))
        {
            hasPii = true;
            categories.Add("credit-card");
        }
    }

    private static string Redact(string text)
    {
        text = ApiKeyRegex().Replace(text, "[REDACTED_API_KEY]");
        text = EmailRegex().Replace(text, "[REDACTED_EMAIL]");
        text = PhoneRegex().Replace(text, "[REDACTED_PHONE]");
        text = SsnRegex().Replace(text, "[REDACTED_SSN]");
        text = CreditCardRegex().Replace(text, "[REDACTED_CC]");
        return text;
    }

    [GeneratedRegex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:\+?1[-.\s]?)?(?:\(?\d{3}\)?[-.\s]?)\d{3}[-.\s]?\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex SsnRegex();

    [GeneratedRegex(@"\b(?:sk-|AKIA|ghp_|xox[baprs]-)[A-Za-z0-9\-_]{8,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex(@"\b(?:\d[ -]*?){13,19}\b", RegexOptions.CultureInvariant)]
    private static partial Regex CreditCardRegex();
}
