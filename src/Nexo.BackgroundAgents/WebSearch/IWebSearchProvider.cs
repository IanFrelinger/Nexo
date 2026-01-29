namespace Nexo.BackgroundAgents.WebSearch;

/// <summary>
/// Interface for web search providers (Bing, Google, DuckDuckGo, etc.).
/// </summary>
public interface IWebSearchProvider
{
    /// <summary>
    /// Provider identifier (e.g. "bing", "duckduckgo").
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Execute a web search and return results.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results (title, URL, snippet), ordered by relevance.</returns>
    Task<IReadOnlyList<WebSearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default);
}
