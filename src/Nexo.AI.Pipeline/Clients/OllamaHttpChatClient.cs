using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Nexo.AI.Pipeline.Clients;

/// <summary>
/// Thin <see cref="IChatClient"/> over Ollama's native <c>/api/chat</c> HTTP API.
/// Avoids registering any Ollama SDK client in DI.
/// </summary>
public sealed class OllamaHttpChatClient : IChatClient, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly string _defaultModel;
    private readonly bool _ownsHttp;

    /// <summary>Creates an Ollama HTTP chat client from pipeline options.</summary>
    public OllamaHttpChatClient(IOptions<MeaiPipelineOptions> options)
        : this(CreateHttpClient(options.Value), ResolveModel(options.Value), ownsHttp: true)
    {
    }

    /// <summary>Creates an Ollama HTTP chat client with an injected <see cref="HttpClient"/> (tests).</summary>
    public OllamaHttpChatClient(HttpClient http, string defaultModel, bool ownsHttp = false)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _defaultModel = string.IsNullOrWhiteSpace(defaultModel) ? "llama3.1:latest" : defaultModel;
        _ownsHttp = ownsHttp;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildRequest(messages, options, stream: false);
        using var response = await _http.PostAsJsonAsync("api/chat", payload, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        var text = body?.Message?.Content ?? string.Empty;
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
        {
            ModelId = body?.Model ?? options?.ModelId ?? _defaultModel,
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var payload = BuildRequest(messages, options, stream: true);
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json"),
        };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line, JsonOptions);
            var content = chunk?.Message?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, content)
                {
                    ModelId = chunk?.Model ?? options?.ModelId ?? _defaultModel,
                };
            }

            if (chunk?.Done == true)
            {
                yield break;
            }
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(ChatClientMetadata))
        {
            return new ChatClientMetadata("ollama", _http.BaseAddress, _defaultModel);
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }

    private object BuildRequest(IEnumerable<ChatMessage> messages, ChatOptions? options, bool stream)
    {
        var model = string.IsNullOrWhiteSpace(options?.ModelId) ? _defaultModel : options!.ModelId!;
        var ollamaMessages = messages.Select(m => new
        {
            role = MapRole(m.Role),
            content = m.Text ?? string.Empty,
        }).ToList();

        return new
        {
            model,
            messages = ollamaMessages,
            stream,
            options = options?.Temperature is null && options?.MaxOutputTokens is null
                ? null
                : new
                {
                    temperature = options?.Temperature,
                    num_predict = options?.MaxOutputTokens,
                },
        };
    }

    private static string MapRole(ChatRole role)
    {
        if (role == ChatRole.System) return "system";
        if (role == ChatRole.Assistant) return "assistant";
        if (role == ChatRole.Tool) return "tool";
        return "user";
    }

    private static HttpClient CreateHttpClient(MeaiPipelineOptions options)
    {
        var baseUrl = ResolveBaseUrl(options);
        return new HttpClient
        {
            BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(300),
        };
    }

    private static string ResolveBaseUrl(MeaiPipelineOptions options)
    {
        var env = Environment.GetEnvironmentVariable("NEXO_OLLAMA_BASE_URL");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env.Trim().TrimEnd('/');
        }

        return string.IsNullOrWhiteSpace(options.OllamaBaseUrl)
            ? "http://localhost:11434"
            : options.OllamaBaseUrl.Trim().TrimEnd('/');
    }

    private static string ResolveModel(MeaiPipelineOptions options)
    {
        var env = Environment.GetEnvironmentVariable("NEXO_OLLAMA_MODEL");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env.Trim();
        }

        return string.IsNullOrWhiteSpace(options.OllamaModel) ? "llama3.1:latest" : options.OllamaModel.Trim();
    }

    private sealed class OllamaChatResponse
    {
        public string? Model { get; set; }
        public OllamaMessage? Message { get; set; }
        public bool Done { get; set; }
    }

    private sealed class OllamaMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }
}
