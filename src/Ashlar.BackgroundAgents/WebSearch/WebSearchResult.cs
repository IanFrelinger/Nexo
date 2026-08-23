namespace Ashlar.BackgroundAgents.WebSearch;

/// <summary>
/// A single web search result.
/// </summary>
/// <param name="Title">Result title.</param>
/// <param name="Url">Result URL.</param>
/// <param name="Snippet">Short snippet/description.</param>
public sealed record WebSearchResult(string Title, string Url, string Snippet);
