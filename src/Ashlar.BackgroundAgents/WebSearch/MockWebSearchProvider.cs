namespace Ashlar.BackgroundAgents.WebSearch;

/// <summary>
/// In-memory web search provider for testing or when no external API is configured.
/// Returns configurable results per query or a default list.
/// </summary>
public sealed class MockWebSearchProvider : IWebSearchProvider
{
    private readonly IReadOnlyList<WebSearchResult> _defaultResults;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<WebSearchResult>> _queryResults;

    /// <inheritdoc />
    public string ProviderId => "mock";

    /// <summary>
    /// Initializes a new instance of the <see cref="MockWebSearchProvider"/> class.
    /// </summary>
    /// <param name="defaultResults">Results to return when query is not in queryResults (optional).</param>
    /// <param name="queryResults">Optional map of query string -> results for deterministic tests.</param>
    public MockWebSearchProvider(
        IReadOnlyList<WebSearchResult>? defaultResults = null,
        IReadOnlyDictionary<string, IReadOnlyList<WebSearchResult>>? queryResults = null)
    {
        _defaultResults = defaultResults ?? Array.Empty<WebSearchResult>();
        _queryResults = queryResults ?? new Dictionary<string, IReadOnlyList<WebSearchResult>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<WebSearchResult>> SearchAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken = default)
    {
        var list = _queryResults.TryGetValue(query ?? string.Empty, out var results)
            ? results
            : _defaultResults;
        var take = list.Take(maxResults).ToList();
        return Task.FromResult<IReadOnlyList<WebSearchResult>>(take);
    }
}
