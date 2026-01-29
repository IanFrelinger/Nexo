using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Abstractions;

namespace Nexo.BackgroundAgents.WebSearch;

/// <summary>
/// ITool that exposes web search to agents. Id: "web_search".
/// Supports sensitive content filtering and domain allowlist/blocklist.
/// </summary>
public sealed class WebSearchTool : ITool
{
    /// <summary>
    /// Default tool id.
    /// </summary>
    public const string DefaultId = "web_search";

    private static readonly ToolSchema SchemaInstance = new(
        DefaultId,
        "Search the web for information. Returns title, URL, and snippet for each result. Supports optional domain filtering.",
        """{"type":"object","properties":{"query":{"type":"string","description":"Search query"},"maxResults":{"type":"integer","description":"Max results (default 10)"}},"required":["query"]}""");

    private readonly IWebSearchProvider _provider;
    private readonly ISensitiveContentFilter? _contentFilter;
    private readonly IReadOnlyList<string>? _allowedDomains;
    private readonly IReadOnlyList<string>? _blockedDomains;
    private readonly int _defaultMaxResults;

    /// <inheritdoc />
    public string Id => DefaultId;

    /// <inheritdoc />
    public ToolSchema Schema => SchemaInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSearchTool"/> class.
    /// </summary>
    /// <param name="provider">Web search provider.</param>
    /// <param name="contentFilter">Optional sensitive content filter (blocks/filters PII in query and snippets).</param>
    /// <param name="allowedDomains">Optional allowlist of domains (e.g. ["wikipedia.org"]). If set, only results from these domains are returned.</param>
    /// <param name="blockedDomains">Optional blocklist of domains. Results from these domains are excluded.</param>
    /// <param name="defaultMaxResults">Default max results when not specified (default 10).</param>
    public WebSearchTool(
        IWebSearchProvider provider,
        ISensitiveContentFilter? contentFilter = null,
        IReadOnlyList<string>? allowedDomains = null,
        IReadOnlyList<string>? blockedDomains = null,
        int defaultMaxResults = 10)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _contentFilter = contentFilter;
        _allowedDomains = allowedDomains;
        _blockedDomains = blockedDomains;
        _defaultMaxResults = defaultMaxResults;
    }

    /// <inheritdoc />
    public async Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
    {
        var args = ParseArgs(toolCall);
        var query = args.Query ?? string.Empty;
        var maxResults = args.MaxResults ?? _defaultMaxResults;

        if (_contentFilter != null)
        {
            if (_contentFilter.ShouldBlockQuery(query))
            {
                var log = new[] { "Web search blocked: query contains sensitive content" };
                var blockedDelta = new ActionDelta(s.Tick, s.Tick + 1, log);
                return new ToolResult(blockedDelta, Array.Empty<WebSearchResult>());
            }
            query = _contentFilter.FilterQuery(query);
        }

        var results = await _provider.SearchAsync(query, maxResults, ct).ConfigureAwait(false);
        var filtered = FilterByDomain(results);

        if (_contentFilter != null && filtered.Count > 0)
        {
            filtered = filtered
                .Select(r => new WebSearchResult(r.Title, r.Url, _contentFilter.FilterSnippet(r.Snippet)))
                .ToList();
        }

        var tick = s.Tick;
        var logMsg = new[] { $"Web search: query='{query}', results={filtered.Count}" };
        var delta = new ActionDelta(tick, tick + 1, logMsg);
        return new ToolResult(delta, filtered);
    }

    private static WebSearchArgs ParseArgs(ToolCall call)
    {
        try
        {
            var json = call.Arguments.GetRawText();
            return JsonSerializer.Deserialize<WebSearchArgs>(json) ?? new WebSearchArgs();
        }
        catch
        {
            return new WebSearchArgs();
        }
    }

    private IReadOnlyList<WebSearchResult> FilterByDomain(IReadOnlyList<WebSearchResult> results)
    {
        if (results.Count == 0)
            return results;

        var allowedSet = _allowedDomains != null
            ? new HashSet<string>(_allowedDomains.Select(NormalizeDomain), StringComparer.OrdinalIgnoreCase)
            : null;
        var blockedSet = _blockedDomains != null
            ? new HashSet<string>(_blockedDomains.Select(NormalizeDomain), StringComparer.OrdinalIgnoreCase)
            : null;

        if (allowedSet == null && blockedSet == null)
            return results;

        var list = new List<WebSearchResult>();
        foreach (var r in results)
        {
            var domain = ExtractDomain(r.Url);
            if (blockedSet != null && blockedSet.Contains(domain))
                continue;
            if (allowedSet != null && !allowedSet.Contains(domain))
                continue;
            list.Add(r);
        }
        return list;
    }

    private static string ExtractDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            var host = uri.Host;
            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                host = host[4..];
            return host;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NormalizeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return string.Empty;
        var d = domain.Trim();
        if (d.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            d = d[4..];
        return d;
    }

    private sealed class WebSearchArgs
    {
        [JsonPropertyName("query")]
        public string? Query { get; set; }
        [JsonPropertyName("maxResults")]
        public int? MaxResults { get; set; }
    }
}
