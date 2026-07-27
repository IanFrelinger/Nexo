using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Nexo.Bricks.DepExtract;

/// <summary>
/// Resolve <c>OLLAMA_MODEL</c> to a content digest for provenance / reproducibility.
/// The deterministic scaffold path remains the fully reproducible offline path;
/// model digests pin which weights produced a draft when the model path is used.
/// </summary>
public static class OllamaModelPin
{
    public sealed record Pin(string Tag, string? Digest)
    {
        public string Display =>
            string.IsNullOrWhiteSpace(Digest) ? Tag : $"{Tag}@{Digest}";
    }

    public static string ResolveTag(string? model) =>
        string.IsNullOrWhiteSpace(model)
            ? (Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen2.5-coder:7b")
            : model.Trim();

    /// <summary>
    /// Parse a configured model string / env without contacting Ollama.
    /// Supports <c>name@sha256:…</c> and <c>OLLAMA_MODEL_DIGEST</c>.
    /// </summary>
    public static Pin ParseConfigured(string? model)
    {
        var tag = ResolveTag(model);
        if (tag.Contains("@sha256:", StringComparison.OrdinalIgnoreCase))
        {
            var at = tag.LastIndexOf("@sha256:", StringComparison.OrdinalIgnoreCase);
            return new Pin(tag[..at], tag[(at + 1)..]);
        }

        var envDigest = Environment.GetEnvironmentVariable("OLLAMA_MODEL_DIGEST");
        if (!string.IsNullOrWhiteSpace(envDigest))
            return new Pin(tag, envDigest.Trim());

        return new Pin(tag, null);
    }

    /// <summary>
    /// If <paramref name="model"/> already looks like <c>name@sha256:…</c>, split it.
    /// Otherwise query the local Ollama tags API for a digest (best-effort).
    /// </summary>
    public static async Task<Pin> ResolveAsync(
        string? model,
        string? baseUrl = null,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        var parsed = ParseConfigured(model);
        var tag = parsed.Tag;
        if (!string.IsNullOrWhiteSpace(parsed.Digest))
            return parsed;

        try
        {
            var url = OllamaEndpointPolicy.ResolveBaseUrl(baseUrl);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var resp = await http.GetAsync(url + "/api/tags", ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                logger?.LogWarning("Could not pin OLLAMA_MODEL digest: tags HTTP {Status}", (int)resp.StatusCode);
                return new Pin(tag, null);
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("models", out var models))
                return new Pin(tag, null);

            foreach (var m in models.EnumerateArray())
            {
                var name = m.TryGetProperty("name", out var n) ? n.GetString() : null;
                if (!string.Equals(name, tag, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(name, tag.Split(':')[0], StringComparison.OrdinalIgnoreCase))
                    continue;
                var digest = m.TryGetProperty("digest", out var d) ? d.GetString() : null;
                if (!string.IsNullOrWhiteSpace(digest))
                {
                    logger?.LogInformation("Pinned OLLAMA_MODEL {Tag} → {Digest}", tag, digest);
                    return new Pin(tag, digest);
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "OLLAMA_MODEL digest resolution failed for {Tag}", tag);
        }

        return new Pin(tag, null);
    }
}
