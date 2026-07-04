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
