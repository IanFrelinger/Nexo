using System.Text.RegularExpressions;

namespace Nexo.BackgroundAgents.WebSearch;

/// <summary>
/// Filters potentially sensitive content from search queries and/or result snippets.
/// Used to reduce risk of leaking PII or sensitive terms in web search.
/// </summary>
public interface ISensitiveContentFilter
{
    /// <summary>
    /// Whether the query should be blocked (e.g. contains known sensitive patterns).
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <returns>True if the query should not be sent to the provider.</returns>
    bool ShouldBlockQuery(string query);

    /// <summary>
    /// Sanitize the query by removing or redacting sensitive patterns.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <returns>Sanitized query safe to send.</returns>
    string FilterQuery(string query);

    /// <summary>
    /// Sanitize a result snippet (e.g. redact emails, phone numbers).
    /// </summary>
    /// <param name="snippet">Snippet text.</param>
    /// <returns>Sanitized snippet.</returns>
    string FilterSnippet(string snippet);
}

/// <summary>
/// Default implementation: blocks/filters common PII patterns (email, phone, SSN-like).
/// </summary>
public sealed class SensitiveContentFilter : ISensitiveContentFilter
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex PhoneRegex = new(
        @"\b\d{3}[-.\s]?\d{3}[-.\s]?\d{4}\b|\b\d{10}\b",
        RegexOptions.Compiled);

    private static readonly Regex SsnLikeRegex = new(
        @"\b\d{3}-\d{2}-\d{4}\b",
        RegexOptions.Compiled);

    /// <summary>OpenAI-style API keys (sk-...).</summary>
    private static readonly Regex ApiKeyRegex = new(
        @"\bsk-[a-zA-Z0-9]{20,}\b",
        RegexOptions.Compiled);

    /// <summary>Credit card-like sequences (4 groups of 4 digits).</summary>
    private static readonly Regex CreditCardLikeRegex = new(
        @"\b\d{4}[\s.-]?\d{4}[\s.-]?\d{4}[\s.-]?\d{4}\b",
        RegexOptions.Compiled);

    private readonly bool _blockQueriesWithPii;
    private readonly string _redactionPlaceholder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SensitiveContentFilter"/> class.
    /// </summary>
    /// <param name="blockQueriesWithPii">If true, ShouldBlockQuery returns true when query contains PII (default true).</param>
    /// <param name="redactionPlaceholder">String to replace sensitive content with (default "[REDACTED]").</param>
    public SensitiveContentFilter(bool blockQueriesWithPii = true, string redactionPlaceholder = "[REDACTED]")
    {
        _blockQueriesWithPii = blockQueriesWithPii;
        _redactionPlaceholder = redactionPlaceholder ?? "[REDACTED]";
    }

    /// <inheritdoc />
    public bool ShouldBlockQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;
        if (!_blockQueriesWithPii)
            return false;
        return ContainsPii(query);
    }

    /// <inheritdoc />
    public string FilterQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return query ?? string.Empty;
        return RedactPii(query);
    }

    /// <inheritdoc />
    public string FilterSnippet(string snippet)
    {
        if (string.IsNullOrWhiteSpace(snippet))
            return snippet ?? string.Empty;
        return RedactPii(snippet);
    }

    private static bool ContainsPii(string text)
    {
        return EmailRegex.IsMatch(text) || PhoneRegex.IsMatch(text) || SsnLikeRegex.IsMatch(text)
            || ApiKeyRegex.IsMatch(text) || CreditCardLikeRegex.IsMatch(text);
    }

    private string RedactPii(string text)
    {
        var result = EmailRegex.Replace(text, _redactionPlaceholder);
        result = PhoneRegex.Replace(result, _redactionPlaceholder);
        result = SsnLikeRegex.Replace(result, _redactionPlaceholder);
        result = ApiKeyRegex.Replace(result, _redactionPlaceholder);
        result = CreditCardLikeRegex.Replace(result, _redactionPlaceholder);
        return result;
    }
}
