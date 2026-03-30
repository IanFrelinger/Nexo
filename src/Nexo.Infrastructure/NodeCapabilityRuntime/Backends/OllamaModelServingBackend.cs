using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Backends;

/// <summary>
/// Ollama-backed implementation of NCR model serving backend.
/// </summary>
public sealed class OllamaModelServingBackend : IModelServingBackend
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public OllamaModelServingBackend(HttpClient httpClient, IOptions<OllamaBackendOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        }
    }

    public BackendType BackendType => BackendType.Ollama;

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync("/api/tags", ct).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<InferenceResult> RunInferenceAsync(InferenceRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var modelId = string.IsNullOrWhiteSpace(request.ModelId) ? throw new ArgumentException("ModelId is required", nameof(request)) : request.ModelId;
        var prompt = request.Prompt ?? string.Empty;
        var system = request.SystemPrompt ?? string.Empty;
        var parameters = request.Parameters ?? new Dictionary<string, object>();
        var started = DateTimeOffset.UtcNow;

        var payload = new
        {
            model = modelId,
            stream = false,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = prompt }
            },
            options = parameters
        };

        using var response = await _httpClient
            .PostAsJsonAsync("/api/chat", payload, JsonOptions, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        var content = doc.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return new InferenceResult
        {
            Output = content,
            TokenCount = ApproximateTokenCount(content),
            Duration = DateTimeOffset.UtcNow - started
        };
    }

    public async Task LoadModelAsync(string modelId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("Model id is required", nameof(modelId));
        var payload = new { model = modelId, prompt = string.Empty, stream = false, keep_alive = "30m" };
        using var response = await _httpClient
            .PostAsJsonAsync("/api/generate", payload, JsonOptions, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task UnloadModelAsync(string modelId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("Model id is required", nameof(modelId));
        var payload = new { model = modelId, prompt = string.Empty, keep_alive = "0s", stream = false };
        using var response = await _httpClient
            .PostAsJsonAsync("/api/generate", payload, JsonOptions, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task PullModelAsync(string modelId, IProgress<PullProgress>? progress = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(modelId)) throw new ArgumentException("Model id is required", nameof(modelId));
        var payload = new { name = modelId, stream = false };
        using var response = await _httpClient
            .PostAsJsonAsync("/api/pull", payload, JsonOptions, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        progress?.Report(new PullProgress { ModelId = modelId, BytesDownloaded = 1, TotalBytes = 1 });
    }

    public async Task<IReadOnlyList<string>> ListLoadedModelsAsync(CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync("/api/ps", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var names = new List<string>();
        foreach (var model in models.EnumerateArray())
        {
            if (model.TryGetProperty("name", out var name))
            {
                var parsed = name.GetString();
                if (!string.IsNullOrWhiteSpace(parsed))
                {
                    names.Add(parsed);
                }
            }
        }

        return names;
    }

    private static int ApproximateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return Math.Max(1, text.Length / 4);
    }
}
