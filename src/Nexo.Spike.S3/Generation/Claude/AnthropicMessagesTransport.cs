using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nexo.Spike.S3.Generation.Claude;

/// <summary>
/// Anthropic Messages API transport for S3 live generation. Reads API key only at call time.
/// </summary>
public sealed class AnthropicMessagesTransport : IS3LlmTransport, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Func<ClaudeRuntimeConfig> _configFactory;

    public AnthropicMessagesTransport(HttpClient? httpClient = null)
        : this(httpClient, ClaudeRuntimeConfig.LoadFromEnvironment)
    {
    }

    internal AnthropicMessagesTransport(HttpClient? httpClient, Func<ClaudeRuntimeConfig> configFactory)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
        _configFactory = configFactory;
    }

    public async Task<string> CompleteAsync(S3LlmCompletionRequest request, CancellationToken ct = default)
    {
        var config = _configFactory();
        config.ValidateForCall();

        var body = new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["max_tokens"] = 4096,
            ["system"] = request.SystemPrompt,
            ["messages"] = new[] { new { role = "user", content = request.UserPrompt } },
            ["temperature"] = request.Temperature
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl.TrimEnd('/')}/messages");
        httpRequest.Headers.Add("x-api-key", config.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        payload = ClaudeRuntimeConfig.RedactSecrets(payload, config);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Anthropic messages API failed with HTTP {(int)response.StatusCode}. Check model id and quota.");
        }

        using var doc = JsonDocument.Parse(payload);
        foreach (var block in doc.RootElement.GetProperty("content").EnumerateArray())
        {
            if (block.TryGetProperty("type", out var typeEl)
                && typeEl.GetString() == "text"
                && block.TryGetProperty("text", out var textEl))
            {
                var text = textEl.GetString()
                           ?? throw new InvalidOperationException("Anthropic text block was empty.");
                return ClaudeRuntimeConfig.RedactSecrets(text, config);
            }
        }

        throw new InvalidOperationException("Anthropic response did not include a text block.");
    }

    public void Dispose() => _httpClient.Dispose();
}
