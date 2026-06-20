using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Nexo.Spike.S2.Adversary.Llm;

/// <summary>
/// HTTP transport for OpenAI- or Anthropic-compatible chat APIs. Reads credentials only at call time.
/// </summary>
public sealed class HttpLlmTransport : ILlmTransport, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Func<LlmRuntimeConfig> _configFactory;

    public HttpLlmTransport(HttpClient? httpClient = null)
        : this(httpClient, LlmRuntimeConfig.LoadFromEnvironment)
    {
    }

    internal HttpLlmTransport(HttpClient? httpClient, Func<LlmRuntimeConfig> configFactory)
    {
        if (httpClient is null)
        {
            httpClient = new HttpClient();
            var timeoutSeconds = int.TryParse(
                Environment.GetEnvironmentVariable("NEXO_S2_LLM_TIMEOUT_SECONDS"),
                out var parsed)
                ? parsed
                : 600;
            httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(30, timeoutSeconds));
        }

        _httpClient = httpClient;
        _configFactory = configFactory;
    }

    public async Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
    {
        var config = _configFactory();
        config.ValidateForCall();

        return config.Provider switch
        {
            LlmProvider.OpenAi => await CompleteOpenAiAsync(config, request, ct).ConfigureAwait(false),
            LlmProvider.Anthropic => await CompleteAnthropicAsync(config, request, ct).ConfigureAwait(false),
            _ => throw new InvalidOperationException(
                "No LLM provider configured. Set OPENAI_API_KEY, ANTHROPIC_API_KEY, or NEXO_LLM_API_KEY.")
        };
    }

    public void Dispose() => _httpClient.Dispose();

    private async Task<string> CompleteOpenAiAsync(
        LlmRuntimeConfig config,
        LlmCompletionRequest request,
        CancellationToken ct)
    {
        var body = new
        {
            model = request.Model,
            temperature = request.Temperature,
            seed = request.Seed,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl.TrimEnd('/')}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI chat completion failed with HTTP {(int)response.StatusCode}. Check model id and quota.");
        }

        using var doc = JsonDocument.Parse(payload);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?? throw new InvalidOperationException("OpenAI response did not include message content.");
    }

    private async Task<string> CompleteAnthropicAsync(
        LlmRuntimeConfig config,
        LlmCompletionRequest request,
        CancellationToken ct)
    {
        var body = new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["max_tokens"] = 4096,
            ["system"] = request.SystemPrompt,
            ["messages"] = new[] { new { role = "user", content = request.UserPrompt } }
        };

        if (request.Temperature is not null)
            body["temperature"] = request.Temperature;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl.TrimEnd('/')}/messages");
        httpRequest.Headers.Add("x-api-key", config.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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
                return textEl.GetString()
                    ?? throw new InvalidOperationException("Anthropic text block was empty.");
            }
        }

        throw new InvalidOperationException("Anthropic response did not include a text block.");
    }
}

internal enum LlmProvider
{
    OpenAi,
    Anthropic
}

internal sealed record LlmRuntimeConfig(
    LlmProvider Provider,
    string ApiKey,
    string BaseUrl,
    string Model,
    double? Temperature,
    int? Seed)
{
    public void ValidateForCall()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException(
                "LLM API key is not set. Set OPENAI_API_KEY, ANTHROPIC_API_KEY, or NEXO_LLM_API_KEY before calling LlmAdversary.");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new InvalidOperationException(
                "LLM model id is not set. Set NEXO_S2_LLM_MODEL before calling LlmAdversary.");
        }
    }

    public static LlmRuntimeConfig LoadFromEnvironment()
    {
        var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var anthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        var genericKey = Environment.GetEnvironmentVariable("NEXO_LLM_API_KEY");
        var providerHint = Environment.GetEnvironmentVariable("NEXO_S2_LLM_PROVIDER");

        LlmProvider provider;
        string apiKey;
        string baseUrl;

        if (!string.IsNullOrWhiteSpace(providerHint))
        {
            provider = providerHint.Trim().ToLowerInvariant() switch
            {
                "openai" => LlmProvider.OpenAi,
                "anthropic" => LlmProvider.Anthropic,
                _ => throw new InvalidOperationException(
                    "NEXO_S2_LLM_PROVIDER must be 'openai' or 'anthropic' when set.")
            };
            apiKey = provider switch
            {
                LlmProvider.OpenAi => openAiKey ?? genericKey ?? string.Empty,
                LlmProvider.Anthropic => anthropicKey ?? genericKey ?? string.Empty,
                _ => string.Empty
            };
            baseUrl = provider switch
            {
                LlmProvider.OpenAi => Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1",
                LlmProvider.Anthropic => Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL") ?? "https://api.anthropic.com/v1",
                _ => string.Empty
            };
        }
        else if (!string.IsNullOrWhiteSpace(openAiKey)
                 || (!string.IsNullOrWhiteSpace(genericKey) && string.IsNullOrWhiteSpace(anthropicKey)))
        {
            provider = LlmProvider.OpenAi;
            apiKey = openAiKey ?? genericKey ?? string.Empty;
            baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
        }
        else if (!string.IsNullOrWhiteSpace(anthropicKey))
        {
            provider = LlmProvider.Anthropic;
            apiKey = anthropicKey;
            baseUrl = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL") ?? "https://api.anthropic.com/v1";
        }
        else
        {
            provider = LlmProvider.OpenAi;
            apiKey = string.Empty;
            baseUrl = "https://api.openai.com/v1";
        }

        var model = Environment.GetEnvironmentVariable("NEXO_S2_LLM_MODEL") ?? string.Empty;
        double? temperature = double.TryParse(Environment.GetEnvironmentVariable("NEXO_S2_LLM_TEMPERATURE"), out var t) ? t : null;
        int? seed = int.TryParse(Environment.GetEnvironmentVariable("NEXO_S2_LLM_SEED"), out var s) ? s : null;

        return new LlmRuntimeConfig(provider, apiKey, baseUrl, model, temperature, seed);
    }
}

public static class LlmTransportFactory
{
    public static ILlmTransport CreateFromEnvironment() => new HttpLlmTransport();
}
