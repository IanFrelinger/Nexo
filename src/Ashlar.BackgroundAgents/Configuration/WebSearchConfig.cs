namespace Ashlar.BackgroundAgents.Configuration;

/// <summary>
/// Web search configuration.
/// </summary>
public class WebSearchConfig
{
    /// <summary>
    /// Whether web search is enabled for this agent.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Search provider ("bing", "google", "duckduckgo", "serpapi").
    /// </summary>
    public string? SearchProvider { get; set; }

    /// <summary>
    /// API key for search provider.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Maximum search results to return.
    /// </summary>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// Whether to filter potentially sensitive content from queries.
    /// </summary>
    public bool FilterSensitiveContent { get; set; } = true;

    /// <summary>
    /// Whitelist of allowed domains (optional).
    /// </summary>
    public List<string>? AllowedDomains { get; set; }

    /// <summary>
    /// Blacklist of blocked domains (optional).
    /// </summary>
    public List<string>? BlockedDomains { get; set; }
}
