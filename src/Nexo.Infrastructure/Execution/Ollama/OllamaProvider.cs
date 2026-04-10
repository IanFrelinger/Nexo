using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Execution.Ollama;

public sealed class OllamaProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;
    private readonly object _manifestLock = new();

    private Dictionary<string, OllamaModelManifest> _manifestByName = new(StringComparer.OrdinalIgnoreCase);

    public OllamaProvider(
        HttpClient httpClient,
        string? baseUrl = null,
        ILogger? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;

        var resolvedBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? "http://localhost:11434"
            : baseUrl;

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(resolvedBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        }

        var initializationResult = RefreshModelsAsync(CancellationToken.None).GetAwaiter().GetResult();
        if (!initializationResult.IsSuccess)
        {
            _logger?.LogWarning(
                "Failed to initialize Ollama model manifest from {BaseUrl}: {Code} {Message}",
                _httpClient.BaseAddress,
                initializationResult.Error?.Code,
                initializationResult.Error?.Message);
        }
    }

    public bool IsAvailable { get; private set; }

    public DateTimeOffset? LastRefreshUtc { get; private set; }

    public IReadOnlyList<OllamaModelManifest> Manifest
    {
        get
        {
            lock (_manifestLock)
            {
                return _manifestByName.Values
                    .OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }
    }

    public async Task<Result<bool>> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var refreshResult = await RefreshModelsAsync(cancellationToken).ConfigureAwait(false);
        if (refreshResult.IsSuccess)
        {
            return Result<bool>.Success(true);
        }

        return Result<bool>.Failure(refreshResult.Error ?? new Error(
            "OLLAMA_HEALTH_UNKNOWN",
            "Ollama health check failed for an unknown reason."));
    }

    public async Task<Result<IReadOnlyList<OllamaModelManifest>>> RefreshModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/tags", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                IsAvailable = false;
                return Result<IReadOnlyList<OllamaModelManifest>>.Failure(new Error(
                    "OLLAMA_TAGS_HTTP_ERROR",
                    $"Ollama /api/tags returned {(int)response.StatusCode} ({response.StatusCode}).",
                    new Dictionary<string, string>
                    {
                        ["statusCode"] = ((int)response.StatusCode).ToString(),
                        ["reasonPhrase"] = response.ReasonPhrase ?? string.Empty
                    }));
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var tags = await JsonSerializer.DeserializeAsync<OllamaTagsResponse>(contentStream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (tags is null)
            {
                IsAvailable = false;
                return Result<IReadOnlyList<OllamaModelManifest>>.Failure(new Error(
                    "OLLAMA_TAGS_EMPTY_RESPONSE",
                    "Ollama /api/tags returned an empty response."));
            }

            var manifest = tags.Models
                .Where(model => !string.IsNullOrWhiteSpace(model.Name))
                .Select(model => new OllamaModelManifest(model.Name, model.Size, model.ModifiedAt))
                .ToArray();

            lock (_manifestLock)
            {
                _manifestByName = manifest.ToDictionary(model => model.Name, StringComparer.OrdinalIgnoreCase);
            }

            LastRefreshUtc = DateTimeOffset.UtcNow;
            IsAvailable = true;
            return Result<IReadOnlyList<OllamaModelManifest>>.Success(manifest);
        }
        catch (OperationCanceledException)
        {
            IsAvailable = false;
            return Result<IReadOnlyList<OllamaModelManifest>>.Failure(new Error(
                "OLLAMA_TAGS_CANCELLED",
                "Ollama /api/tags request was cancelled."));
        }
        catch (HttpRequestException ex)
        {
            IsAvailable = false;
            return Result<IReadOnlyList<OllamaModelManifest>>.Failure(new Error(
                "OLLAMA_UNREACHABLE",
                $"Unable to reach Ollama /api/tags endpoint at {_httpClient.BaseAddress}: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            IsAvailable = false;
            return Result<IReadOnlyList<OllamaModelManifest>>.Failure(new Error(
                "OLLAMA_TAGS_INVALID_JSON",
                $"Failed to deserialize Ollama /api/tags response: {ex.Message}"));
        }
    }

    public Result<OllamaModelManifest> ValidateModel(string requestedModel)
    {
        if (string.IsNullOrWhiteSpace(requestedModel))
        {
            return Result<OllamaModelManifest>.Failure(new Error(
                "OLLAMA_MODEL_REQUIRED",
                "A model name is required for Ollama inference."));
        }

        if (!IsAvailable)
        {
            return Result<OllamaModelManifest>.Failure(new Error(
                "OLLAMA_UNAVAILABLE",
                "Ollama provider is unavailable. Run health check or refresh the model manifest."));
        }

        lock (_manifestLock)
        {
            if (_manifestByName.TryGetValue(requestedModel, out var model))
            {
                return Result<OllamaModelManifest>.Success(model);
            }

            var availableModels = string.Join(", ", _manifestByName.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
            return Result<OllamaModelManifest>.Failure(new Error(
                "OLLAMA_MODEL_NOT_FOUND",
                $"Requested Ollama model '{requestedModel}' was not found in the discovered model manifest.",
                new Dictionary<string, string>
                {
                    ["requestedModel"] = requestedModel,
                    ["availableModels"] = availableModels
                }));
        }
    }

    public async Task<Result<string>> ExecuteChatAsync(
        string requestedModel,
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<byte[]>? imageBytesList,
        CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateModel(requestedModel);
        if (!validationResult.IsSuccess || validationResult.Value is null)
        {
            return Result<string>.Failure(validationResult.Error ?? new Error(
                "OLLAMA_MODEL_VALIDATION_FAILED",
                "Model validation failed before Ollama execution."));
        }

        object userMessage;
        if (imageBytesList is { Count: > 0 })
        {
            var imageBase64Array = imageBytesList
                .Where(bytes => bytes != null && bytes.Length > 0)
                .Select(Convert.ToBase64String)
                .ToArray();

            userMessage = new
            {
                role = "user",
                content = userPrompt ?? string.Empty,
                images = imageBase64Array
            };
        }
        else
        {
            userMessage = new
            {
                role = "user",
                content = userPrompt ?? string.Empty
            };
        }

        var payload = new
        {
            model = validationResult.Value.Name,
            stream = false,
            messages = new[]
            {
                new { role = "system", content = systemPrompt ?? string.Empty },
                userMessage
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = new StringContent(json, Encoding.UTF8)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Result<string>.Failure(new Error(
                    response.StatusCode == HttpStatusCode.NotFound ? "OLLAMA_CHAT_MODEL_NOT_FOUND" : "OLLAMA_CHAT_HTTP_ERROR",
                    $"Ollama /api/chat returned {(int)response.StatusCode} ({response.StatusCode})."));
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var jsonDocument = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!jsonDocument.RootElement.TryGetProperty("message", out var messageElement)
                || !messageElement.TryGetProperty("content", out var contentElement))
            {
                return Result<string>.Failure(new Error(
                    "OLLAMA_CHAT_INVALID_RESPONSE",
                    "Ollama /api/chat response did not contain message.content."));
            }

            var content = contentElement.GetString() ?? string.Empty;
            return Result<string>.Success(content);
        }
        catch (OperationCanceledException)
        {
            return Result<string>.Failure(new Error(
                "OLLAMA_CHAT_CANCELLED",
                "Ollama /api/chat request was cancelled."));
        }
        catch (HttpRequestException ex)
        {
            IsAvailable = false;
            return Result<string>.Failure(new Error(
                "OLLAMA_UNREACHABLE",
                $"Unable to reach Ollama /api/chat endpoint at {_httpClient.BaseAddress}: {ex.Message}"));
        }
        catch (JsonException ex)
        {
            return Result<string>.Failure(new Error(
                "OLLAMA_CHAT_INVALID_JSON",
                $"Failed to parse Ollama /api/chat response: {ex.Message}"));
        }
    }
}
