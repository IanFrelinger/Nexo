namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Normalizes user-supplied base URLs to a full OpenAI-style chat completions URL.
/// </summary>
public static class OpenAiCompatibleEndpoint
{
    /// <summary>
    /// Returns an absolute URI for <c>POST .../v1/chat/completions</c>.
    /// Accepts a full completions URL, an API root ending in <c>/v1</c>, or an origin only.
    /// </summary>
    public static Uri NormalizeChatCompletionsUrl(string baseUrlOrFull)
    {
        if (string.IsNullOrWhiteSpace(baseUrlOrFull))
        {
            throw new ArgumentException("Base URL is required.", nameof(baseUrlOrFull));
        }

        var trimmed = baseUrlOrFull.Trim();
        if (!trimmed.Contains("://", StringComparison.Ordinal))
        {
            trimmed = "http://" + trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL.", nameof(baseUrlOrFull));
        }

        var path = uri.AbsolutePath.TrimEnd('/');
        if (path.EndsWith("/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return uri;
        }

        if (path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return uri;
        }

        if (path.Equals("/v1", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            var origin = uri.GetLeftPart(UriPartial.Authority);
            var basePath = path.EndsWith("/", StringComparison.Ordinal) ? path : path + "/";
            return new Uri(origin + basePath.TrimEnd('/') + "/chat/completions");
        }

        var root = uri.GetLeftPart(UriPartial.Authority);
        return new Uri(root + (path.Length > 0 ? path + "/" : "/") + "v1/chat/completions");
    }
}
