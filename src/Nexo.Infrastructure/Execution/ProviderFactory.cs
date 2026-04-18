using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Ephemeral.Ports;
using Nexo.Core.Domain;
using Nexo.Infrastructure.Execution.Ollama;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Central factory for resolving and instantiating LLM provider backends.
///
/// <para><b>Role in execution pipeline:</b> The orchestration layer calls
/// <see cref="IProviderFactory"/> to obtain a provider for a given request.
/// The factory decides which backend to use based on the provider name
/// resolved through a precedence chain: explicit caller argument → environment
/// variables (e.g. <c>NEXO_PROVIDER</c>) → user configuration → compile-time
/// defaults in <see cref="NexoDefaults"/>.</para>
///
/// <para><b>Available providers:</b> openai, azure, ollama, local
/// (in-process ONNX/LLamaSharp), video (SmolVLM2-Video in Docker), and the
/// test-only mock/offline/mock-json/echo set. To add a new provider, add its
/// key to <c>_availableProviders</c>, implement availability detection in
/// <see cref="IsProviderAvailable"/>, and add instantiation logic in the
/// provider-creation path.</para>
///
/// <para><b>Retry policy:</b> A static Polly <see cref="AsyncRetryPolicy{T}"/>
/// applies exponential back-off (2^attempt seconds) for HTTP 5xx,
/// <c>429 TooManyRequests</c>, <see cref="HttpRequestException"/>, and
/// <see cref="TaskCanceledException"/>. The retry count is read once at type
/// initialization from <c>NEXO_LLM_RETRY_COUNT</c> (non-negative int), falling
/// back to <see cref="NexoDefaults.LlmRetryCount"/>.</para>
///
/// <para><b>Mock provider gating:</b> Mock/offline providers are only available
/// when <c>NEXO_ALLOW_MOCK=1</c>. This prevents accidental use in production
/// while allowing integration tests to opt-in.</para>
///
/// <para><b>Thread-safety:</b> <c>Http</c> and <c>HttpRetryPolicy</c> are static
/// and safe for concurrent use. The Ollama provider instance is lazily cached
/// behind <c>_ollamaProviderLock</c> and re-created only when the base URL
/// changes (e.g. ephemeral container restart).</para>
/// </summary>
public class ProviderFactory : IProviderFactory
{
    private readonly ILogger<ProviderFactory> _logger;
    private readonly IEphemeralModelLifecycle? _ephemeralLifecycle;
    private readonly object _ollamaProviderLock = new();
    private OllamaProvider? _ollamaProvider;
    private string? _ollamaProviderBaseUrl;
    private HttpClient? _ollamaHttpClient;
    private static readonly HttpClient Http = new();
    private static readonly AsyncRetryPolicy<HttpResponseMessage> HttpRetryPolicy = CreateHttpRetryPolicy();

    // --- Retry policy (static, initialized once per AppDomain) ---

    private static AsyncRetryPolicy<HttpResponseMessage> CreateHttpRetryPolicy()
    {
        var maxRetries = int.TryParse(Environment.GetEnvironmentVariable("NEXO_LLM_RETRY_COUNT"), out var c) && c >= 0 ? c : NexoDefaults.LlmRetryCount;

        return Policy
            .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.TooManyRequests)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                maxRetries,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    // --- Provider catalogue ---

    private readonly HashSet<string> _availableProviders = new()
    {
        "openai",
        "azure",
        "ollama",
        "local", // In-process ONNX/LLamaSharp; requires NEXO_LOCAL_MODEL_PATH
        "video", // SmolVLM2-Video in Docker; requires VIDEO_SERVICE_URL
        "mock",
        "offline",
        "mock-json",
        "echo"
    };

    private static bool AllowMock => string.Equals(Environment.GetEnvironmentVariable("NEXO_ALLOW_MOCK"), "1", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a new <see cref="ProviderFactory"/> and eagerly initializes the
    /// Ollama provider. Eager init is intentional: Ollama requires pulling a
    /// model manifest on first contact, which is slow (~seconds). Doing this at
    /// construction time avoids a latency spike on the first user request.
    /// Failure is non-fatal — Ollama simply won't be available until the next
    /// lazy attempt.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="ephemeralLifecycle">Optional ephemeral model lifecycle. When NEXO_EPHEMERAL_MODELS=1, use to resolve Ollama URL from container.</param>
    public ProviderFactory(ILogger<ProviderFactory> logger, IEphemeralModelLifecycle? ephemeralLifecycle = null)
    {
        _logger = logger;
        _ephemeralLifecycle = ephemeralLifecycle;

        try
        {
            var baseUrl = GetOllamaBaseUrlAsync(CancellationToken.None).GetAwaiter().GetResult();
            _ = GetOrCreateOllamaProvider(baseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Ollama provider manifest during ProviderFactory startup.");
        }
    }
    
    /// <inheritdoc />
    public bool IsProviderAvailable(string provider)
    {
        provider = (provider ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(provider)) return false;

        if (!_availableProviders.Contains(provider)) return false;

        // Mock/offline providers: only available when NEXO_ALLOW_MOCK=1 (default: disabled)
        if (provider is "mock" or "offline" or "mock-json" or "echo")
            return AllowMock;

        return provider switch
        {
            "openai" => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
            "azure" => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"))
                       && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"))
                       && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")),
            "ollama" => IsOllamaProviderAvailable(),
            "local" => LocalModelProvider.IsAvailable(),
            "video" => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VIDEO_SERVICE_URL")),
            _ => false
        };
    }
    
    /// <inheritdoc />
    public async Task<string> ExecuteLLMAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        object config,
        CancellationToken cancellationToken = default)
    {
        var defaultProvider = AllowMock ? "mock" : "ollama";
        provider = (provider ?? defaultProvider).Trim().ToLowerInvariant();
        _logger.LogInformation("Executing LLM request with provider {Provider}", provider);

        // Mock/offline providers: only when NEXO_ALLOW_MOCK=1
        if (provider is "mock" or "offline" or "mock-json" or "echo")
        {
            if (!AllowMock)
                throw new ModelUnavailableException("Mock providers are disabled. Set NEXO_ALLOW_MOCK=1 for tests/demos, or use a real provider (ollama, openai, azure, local).");
            await Task.Delay(NexoDefaults.MockDelayMs, cancellationToken);
            return GenerateMockJsonResponse(systemPrompt, userPrompt);
        }

        await Task.Delay(NexoDefaults.MockDelayMs, cancellationToken);
        
        // Real providers: fail fast on misconfiguration or request failure (no mock fallback).
        if (provider is "openai")
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OPENAI_API_KEY is not set. Set it or use provider mock/offline.");
            return await ExecuteOpenAiAsync(apiKey, systemPrompt, userPrompt, cancellationToken);
        }

        if (provider is "azure")
        {
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deployment))
                throw new InvalidOperationException("Azure OpenAI env vars not set (AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY, AZURE_OPENAI_DEPLOYMENT). Set them or use provider mock/offline.");
            return await ExecuteAzureOpenAiAsync(endpoint, apiKey, deployment, systemPrompt, userPrompt, cancellationToken);
        }

        if (provider is "ollama")
            return await ExecuteOllamaAsync(systemPrompt, userPrompt, null, config, cancellationToken);

        if (provider is "local")
            return await LocalModelProvider.ExecuteAsync(systemPrompt, userPrompt, config, cancellationToken);

        throw new InvalidOperationException($"Unknown or unsupported provider: {provider}. Use ollama, openai, azure, local, or mock (NEXO_ALLOW_MOCK=1).");
    }

    private async Task<string> ExecuteOpenAiAsync(string apiKey, string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? NexoDefaults.OpenAiDefaultModel;
        var url = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? NexoDefaults.OpenAiDefaultBaseUrl;

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt ?? "" },
                new { role = "user", content = userPrompt ?? "" }
            },
            temperature = NexoDefaults.LlmTemperature
        };
        var json = JsonSerializer.Serialize(payload);

        using var resp = await HttpRetryPolicy.ExecuteAsync(() =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = new StringContent(json);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return Http.SendAsync(req, ct);
        });
        var body = await resp.Content.ReadAsStringAsync(ct);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? throw new InvalidOperationException("OpenAI response content was null");
    }

    private async Task<string> ExecuteAzureOpenAiAsync(string endpoint, string apiKey, string deployment, string systemPrompt, string userPrompt, CancellationToken ct)
    {
        endpoint = endpoint.TrimEnd('/');
        var apiVersion = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION") ?? NexoDefaults.AzureOpenAiDefaultApiVersion;
        var url = $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}";

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt ?? "" },
                new { role = "user", content = userPrompt ?? "" }
            },
            temperature = NexoDefaults.LlmTemperature
        };
        var json = JsonSerializer.Serialize(payload);

        using var resp = await HttpRetryPolicy.ExecuteAsync(() =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("api-key", apiKey);
            req.Content = new StringContent(json);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return Http.SendAsync(req, ct);
        });
        var body = await resp.Content.ReadAsStringAsync(ct);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? throw new InvalidOperationException("Azure OpenAI response content was null");
    }

    private async Task<string> ExecuteOpenAiVisionAsync(string apiKey, string systemPrompt, string userPrompt, byte[]? imageBytes, object? config, CancellationToken ct)
    {
        var model = Environment.GetEnvironmentVariable("OPENAI_VISION_MODEL") ?? Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? NexoDefaults.OpenAiDefaultVisionModel;
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com";
        var url = baseUrl.EndsWith("/v1/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : baseUrl.TrimEnd('/') + "/v1/chat/completions";

        object[] contentParts;
        if (imageBytes is { Length: > 0 })
        {
            var b64 = Convert.ToBase64String(imageBytes);
            contentParts = new object[] { new { type = "text", text = userPrompt ?? "" }, new { type = "image_url", image_url = new { url = $"data:image/png;base64,{b64}" } } };
        }
        else
        {
            contentParts = new object[] { new { type = "text", text = userPrompt ?? "" } };
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var messages = new object[]
        {
            new { role = "system", content = systemPrompt ?? "" },
            new { role = "user", content = (object)contentParts }
        };
        var payload = new { model, messages, temperature = NexoDefaults.LlmTemperature, max_tokens = NexoDefaults.LlmMaxTokens };

        req.Content = new StringContent(JsonSerializer.Serialize(payload));
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var resp = await Http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? throw new InvalidOperationException("OpenAI vision response content was null");
    }

    private async Task<string> ExecuteAzureOpenAiVisionAsync(string endpoint, string apiKey, string deployment, string systemPrompt, string userPrompt, byte[]? imageBytes, object? config, CancellationToken ct)
    {
        endpoint = endpoint.TrimEnd('/');
        var apiVersion = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION") ?? NexoDefaults.AzureOpenAiDefaultApiVersion;
        var url = $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}";

        object[] contentParts;
        if (imageBytes is { Length: > 0 })
        {
            var b64 = Convert.ToBase64String(imageBytes);
            contentParts = new object[] { new { type = "text", text = userPrompt ?? "" }, new { type = "image_url", image_url = new { url = $"data:image/png;base64,{b64}" } } };
        }
        else
        {
            contentParts = new object[] { new { type = "text", text = userPrompt ?? "" } };
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("api-key", apiKey);

        var messages = new object[]
        {
            new { role = "system", content = systemPrompt ?? "" },
            new { role = "user", content = (object)contentParts }
        };
        var payload = new { messages, temperature = NexoDefaults.LlmTemperature, max_tokens = NexoDefaults.LlmMaxTokens };

        req.Content = new StringContent(JsonSerializer.Serialize(payload));
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var resp = await Http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? throw new InvalidOperationException("Azure OpenAI vision response content was null");
    }

    private static string? GetModelFromConfig(object? config)
    {
        if (config == null) return null;
        if (config is IReadOnlyDictionary<string, object> rod && rod.TryGetValue("model", out var v))
            return v as string;
        var t = config.GetType();
        var prop = t.GetProperty("model", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
            ?? t.GetProperty("Model", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        return prop?.GetValue(config) as string;
    }

    private async Task<string> ExecuteOllamaAsync(string systemPrompt, string userPrompt, IReadOnlyList<byte[]>? imageBytesList, object? config, CancellationToken ct)
    {
        var baseUrl = await GetOllamaBaseUrlAsync(ct);
        var hasImages = imageBytesList is { Count: > 0 };
        var configModel = GetModelFromConfig(config);
        var requestedModel = configModel
            ?? (hasImages
                ? (Environment.GetEnvironmentVariable("OLLAMA_VISION_MODEL") ?? Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? NexoDefaults.OllamaDefaultVisionModel)
                : (Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? NexoDefaults.OllamaDefaultModel));
        var ollamaProvider = GetOrCreateOllamaProvider(baseUrl);

        var validation = ollamaProvider.ValidateModel(requestedModel);
        if (!validation.IsSuccess)
        {
            var refresh = await ollamaProvider.RefreshModelsAsync(ct).ConfigureAwait(false);
            if (!refresh.IsSuccess)
            {
                throw new ModelUnavailableException(
                    $"Ollama manifest refresh failed. {FormatOllamaError(refresh.Error)}");
            }

            validation = ollamaProvider.ValidateModel(requestedModel);
            if (!validation.IsSuccess)
            {
                throw new ModelUnavailableException(
                    $"Ollama model validation failed. {FormatOllamaError(validation.Error)}");
            }
        }

        var execution = await ollamaProvider
            .ExecuteChatAsync(requestedModel, systemPrompt, userPrompt, imageBytesList, ct)
            .ConfigureAwait(false);
        if (!execution.IsSuccess)
        {
            throw new ModelUnavailableException(
                $"Ollama execution failed. {FormatOllamaError(execution.Error)}");
        }

        return execution.Value ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<string> ExecuteVisionAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        byte[] imageBytes,
        object config,
        CancellationToken cancellationToken = default)
    {
        var defaultProvider = AllowMock ? "mock" : "ollama";
        provider = (provider ?? defaultProvider).Trim().ToLowerInvariant();
        _logger.LogInformation("Executing vision request with provider {Provider}", provider);

        if (provider is "mock" or "offline" or "mock-json" or "echo")
        {
            if (!AllowMock)
                throw new ModelUnavailableException("Mock providers are disabled. Set NEXO_ALLOW_MOCK=1 or use ollama, openai, azure.");
            return GenerateMockJsonResponse(systemPrompt, userPrompt);
        }

        if (provider is "ollama" or "auto" or "local")
            return await ExecuteOllamaAsync(systemPrompt, userPrompt, imageBytes != null ? [imageBytes] : null, config, cancellationToken);

        if (provider is "openai")
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OPENAI_API_KEY is not set. Set it or use provider ollama.");
            return await ExecuteOpenAiVisionAsync(apiKey, systemPrompt, userPrompt, imageBytes, config, cancellationToken);
        }

        if (provider is "azure")
        {
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deployment))
                throw new InvalidOperationException("Azure OpenAI env vars not set (AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY, AZURE_OPENAI_DEPLOYMENT). Set them or use provider ollama.");
            return await ExecuteAzureOpenAiVisionAsync(endpoint, apiKey, deployment, systemPrompt, userPrompt, imageBytes, config, cancellationToken);
        }

        throw new InvalidOperationException($"Unknown or unsupported vision provider: {provider}. Use ollama, openai, azure, auto, or local.");
    }

    /// <inheritdoc />
    public async Task<string> ExecuteVisionMultiFrameAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<byte[]> frameBytes,
        object config,
        CancellationToken cancellationToken = default)
    {
        var defaultProvider = AllowMock ? "mock" : "ollama";
        provider = (provider ?? defaultProvider).Trim().ToLowerInvariant();
        var frames = (frameBytes ?? Array.Empty<byte[]>()).Where(b => b != null && b.Length > 0).ToList();

        if (frames.Count == 0)
            throw new ArgumentException("At least one non-empty frame is required.", nameof(frameBytes));

        // Single frame: delegate to existing path
        if (frames.Count == 1)
            return await ExecuteVisionAsync(provider, systemPrompt, userPrompt, frames[0], config, cancellationToken);

        _logger.LogInformation("Executing multi-frame vision request with provider {Provider}, {Count} frames", provider, frames.Count);

        if (provider is "mock" or "offline" or "mock-json" or "echo")
        {
            if (!AllowMock)
                throw new ModelUnavailableException("Mock providers are disabled. Set NEXO_ALLOW_MOCK=1 or use ollama, openai, azure.");
            return await ExecuteVisionAsync(provider, systemPrompt, userPrompt + $"\n[Note: {frames.Count} frames provided, analyzing most recent.]", frames[^1], config, cancellationToken);
        }

        if (provider is "ollama" or "auto" or "local")
            return await ExecuteOllamaAsync(systemPrompt, userPrompt, frames, config, cancellationToken);

        if (provider is "video")
            return await ExecuteVideoAsync(systemPrompt, userPrompt, frames, config, cancellationToken);

        if (provider is "openai")
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OPENAI_API_KEY is not set. Set it or use provider ollama.");
            return await ExecuteOpenAiVisionAsync(apiKey, systemPrompt, userPrompt, frames[^1], config, cancellationToken);
        }

        if (provider is "azure")
        {
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deployment))
                throw new InvalidOperationException("Azure OpenAI env vars not set. Set them or use provider ollama.");
            return await ExecuteAzureOpenAiVisionAsync(endpoint, apiKey, deployment, systemPrompt, userPrompt, frames[^1], config, cancellationToken);
        }

        throw new InvalidOperationException($"Unknown or unsupported vision provider: {provider}.");
    }

    /// <inheritdoc />
    public async Task<string> ExecuteVideoAsync(
        string systemPrompt,
        string userPrompt,
        IReadOnlyList<byte[]> frameBytes,
        object config,
        CancellationToken cancellationToken = default)
    {
        var frames = (frameBytes ?? Array.Empty<byte[]>()).Where(b => b != null && b.Length > 0).ToList();
        if (frames.Count == 0)
            throw new ArgumentException("At least one frame is required for video analysis.", nameof(frameBytes));

        var baseUrl = (Environment.GetEnvironmentVariable("VIDEO_SERVICE_URL") ?? "").TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException("VIDEO_SERVICE_URL is not set. Start the SmolVLM2 video container and set VIDEO_SERVICE_URL.");

        var fps = NexoDefaults.VideoDefaultFps;
        var tmpDir = Path.Combine(Path.GetTempPath(), "nexo-video-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            for (var i = 0; i < frames.Count; i++)
                await File.WriteAllBytesAsync(Path.Combine(tmpDir, $"frame_{i:D5}.png"), frames[i], cancellationToken);

            var mp4Path = Path.Combine(tmpDir, "clip.mp4");
            var ffmpeg = FindFfmpeg();
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpeg,
                ArgumentList =
                {
                    "-y", "-framerate", fps.ToString(), "-i", Path.Combine(tmpDir, "frame_%05d.png"),
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-t", (frames.Count / (double)fps).ToString("F2"), mp4Path
                },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null)
                throw new InvalidOperationException("Failed to start ffmpeg. Install ffmpeg.");
            var err = await proc.StandardError.ReadToEndAsync(cancellationToken);
            await proc.WaitForExitAsync(cancellationToken);
            if (proc.ExitCode != 0 || !File.Exists(mp4Path))
                throw new InvalidOperationException($"ffmpeg failed: {err}");

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(userPrompt), "prompt");
            content.Add(new StringContent(systemPrompt), "system_prompt");
            var videoBytes = await File.ReadAllBytesAsync(mp4Path, cancellationToken);
            content.Add(new ByteArrayContent(videoBytes) { Headers = { ContentType = new MediaTypeHeaderValue("video/mp4") } }, "video", "clip.mp4");

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/analyze") { Content = content };
            using var resp = await Http.SendAsync(req, cancellationToken);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Video service error {resp.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var summary = doc.RootElement.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : body;
            var understanding = new
            {
                screenType = "Video",
                currentContext = summary,
                availableActions = Array.Empty<object>(),
                currentObjective = "Observation",
                progressPercent = 0,
                issues = Array.Empty<object>(),
                unexploredAreas = Array.Empty<string>(),
                confidence = 0.9
            };
            return JsonSerializer.Serialize(understanding);
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { /* ignore */ }
        }
    }

    private static string FindFfmpeg()
    {
        var name = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            var full = Path.Combine(dir.Trim(), name);
            if (File.Exists(full)) return full;
        }
        return name;
    }

    private async Task<string> GetOllamaBaseUrlAsync(CancellationToken ct)
    {
        if (_ephemeralLifecycle != null)
            return await _ephemeralLifecycle.GetBaseUrlAsync(ct);
        var url = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? NexoDefaults.OllamaDefaultBaseUrl;
        return url.TrimEnd('/');
    }

    private bool IsOllamaProviderAvailable()
    {
        try
        {
            var baseUrl = GetOllamaBaseUrlAsync(CancellationToken.None).GetAwaiter().GetResult();
            var provider = GetOrCreateOllamaProvider(baseUrl);
            var health = provider.CheckHealthAsync(CancellationToken.None).GetAwaiter().GetResult();
            return health.IsSuccess && health.Value == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to run Ollama health check.");
            return false;
        }
    }

    private OllamaProvider GetOrCreateOllamaProvider(string baseUrl)
    {
        lock (_ollamaProviderLock)
        {
            if (_ollamaProvider != null
                && string.Equals(_ollamaProviderBaseUrl, baseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return _ollamaProvider;
            }

            var timeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("OLLAMA_TIMEOUT_SECONDS"), out var configuredTimeout)
                && configuredTimeout > 0
                ? configuredTimeout
                : 300;

            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            if (_ollamaHttpClient is not null)
            {
                _ollamaHttpClient.Dispose();
            }

            _ollamaHttpClient = httpClient;
            _ollamaProvider = new OllamaProvider(httpClient, baseUrl, _logger);
            _ollamaProviderBaseUrl = baseUrl;
            return _ollamaProvider;
        }
    }

    private static string FormatOllamaError(Error? error)
    {
        if (error is null)
        {
            return "Unknown Ollama error.";
        }

        var metadataSuffix = error.Metadata is { Count: > 0 }
            ? $" Metadata: {string.Join(", ", error.Metadata.Select(kvp => $"{kvp.Key}={kvp.Value}"))}."
            : string.Empty;

        return $"{error.Code}: {error.Message}{metadataSuffix}";
    }

    /// <inheritdoc />
    public async Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default)
    {
        var baseUrl = await GetOllamaBaseUrlAsync(cancellationToken);
        var provider = GetOrCreateOllamaProvider(baseUrl);
        var healthResult = await provider.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
        if (!healthResult.IsSuccess || healthResult.Value != true)
        {
            throw new InvalidOperationException(
                $"Ollama is not reachable at {baseUrl}. {FormatOllamaError(healthResult.Error)}");
        }

        var models = provider.Manifest.Select(m => m.Name).ToList();
        if (models.Count == 0)
            throw new InvalidOperationException(
                $"Ollama at {baseUrl} returned no models. Pull a model: ollama pull llama3.2:3b (and for vision: ollama pull richardyoung/smolvlm2-2.2b-instruct or llava:7b).");

        if (requireVisionModel)
        {
            var hasVision = models.Any(m => m.Contains("smolvlm", StringComparison.OrdinalIgnoreCase) || m.Contains("llava", StringComparison.OrdinalIgnoreCase));
            if (!hasVision)
                throw new InvalidOperationException(
                    $"No vision model found at {baseUrl}. Available: {string.Join(", ", models)}. Pull one: ollama pull richardyoung/smolvlm2-2.2b-instruct (or llava:7b)");
        }
    }

    private static string GenerateMockJsonResponse(string systemPrompt, string userPrompt)
    {
        systemPrompt ??= "";
        userPrompt ??= "";

        // Orchestration: Architect decomposition schema
        if (systemPrompt.Contains("Architect Agent that decomposes complex requests", StringComparison.OrdinalIgnoreCase))
        {
            var request =
                Regex.Match(userPrompt, @"^\s*Request:\s*(?<req>.+?)\s*$", RegexOptions.Multiline).Groups["req"].Value.Trim();
            if (string.IsNullOrWhiteSpace(request))
            {
                request = Regex.Match(userPrompt, @"^\s*Original request:\s*(?<req>.+?)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                    .Groups["req"].Value.Trim();
            }
            if (string.IsNullOrWhiteSpace(request))
            {
                request = "request";
            }

            // Minimal, schema-compliant decomposition with one agent.
            // This keeps orchestration functional in offline/demo mode without network access.
            var agent = new Dictionary<string, object?>
            {
                ["agentId"] = "gameplay-1",
                ["domain"] = "Gameplay",
                ["goal"] = $"Handle request: {request}",
                ["description"] = "Offline/mock decomposition (single-agent). Expand domains when using a real provider.",
                ["dependencies"] = Array.Empty<string>(),
                // IMPORTANT: omit outputSchema entirely (null triggers schema validation errors downstream)
                ["constraints"] = Array.Empty<object>(),
                ["resourceRequirements"] = new
                {
                    estimatedComputeSeconds = 30,
                    requiredContextTokens = 1000,
                    requiredMemoryMB = 256
                },
                ["priority"] = 1
            };

            var obj = new Dictionary<string, object?>
            {
                ["agents"] = new[] { agent },
                ["reasoning"] = "Offline/mock-json provider produced a minimal valid decomposition.",
                ["confidence"] = 0.55
            };

            return JsonSerializer.Serialize(obj);
        }

        // Universal Tester bricks
        if (systemPrompt.Contains("universal testing agent analyzing", StringComparison.OrdinalIgnoreCase))
        {
            // UnderstandingBrick schema
            var obj = new
            {
                screenType = InferScreenType(userPrompt),
                currentContext = "Offline analysis (mock-json provider)",
                availableActions = Array.Empty<object>(),
                currentObjective = "Gather baseline evidence",
                progressPercent = InferProgressPercent(userPrompt),
                issues = Array.Empty<object>(),
                unexploredAreas = Array.Empty<string>(),
                confidence = 0.6
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("deciding what action to take next in testing", StringComparison.OrdinalIgnoreCase))
        {
            var nextActionId = InferNextActionId(userPrompt);
            var obj = new
            {
                nextActionId,
                reasoning = "Offline/mock decision: pick first available action if any; otherwise wait",
                shouldStop = nextActionId == "wait"
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("validating the result of a test action", StringComparison.OrdinalIgnoreCase))
        {
            var success = Regex.IsMatch(userPrompt, @"Execution Success:\s*True", RegexOptions.IgnoreCase);
            var obj = new
            {
                passed = success,
                reasoning = success ? "No errors indicated by execution result." : "Execution reported failure.",
                issues = success
                    ? Array.Empty<object>()
                    : new[] { new { type = "error", description = "Action execution failed", severity = "high" } },
                confidence = 0.7
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("generating a test report summary", StringComparison.OrdinalIgnoreCase))
        {
            var obj = new
            {
                findings = new[] { "Offline/mock report: summary generated without network access." },
                recommendations = new[] { "Wire a real provider for richer summaries." }
            };
            return JsonSerializer.Serialize(obj);
        }

        // Self-extend tool-calling agent (used by background-agent extender + self-extend CLI command).
        if (systemPrompt.Contains("You are a self-extending code agent", StringComparison.OrdinalIgnoreCase))
        {
            var objective = ExtractObjective(userPrompt);
            if (LooksLikeUiFeatureHotloadObjective(objective))
            {
                return BuildUiFeatureHotloadToolCallsJson(systemPrompt, objective);
            }
            if (LooksLikeUnityBootstrapObjective(objective))
            {
                return BuildUnityBootstrapToolCallsJson(systemPrompt, objective);
            }
            if (LooksLikeUiDemoObjective(objective))
            {
                return BuildUiDemoToolCallsJson(systemPrompt);
            }
            if (LooksLikePersonalSoftwareObjective(objective))
            {
                return BuildPersonalAppToolCallsJson(systemPrompt);
            }

            // Explicitly return an empty tool call envelope for schema consistency.
            return JsonSerializer.Serialize(new { tool_calls = Array.Empty<object>() });
        }

        // Fallback: return a benign JSON object
        return "{}";
    }

    private static string ExtractObjective(string userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
            return string.Empty;
        var match = Regex.Match(userPrompt, @"Objective:\s*(?<goal>[\s\S]+)$", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["goal"].Value.Trim() : userPrompt.Trim();
    }

    private static bool LooksLikeUnityBootstrapObjective(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;
        return objective.Contains("unity", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("mono", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("dash ability", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("gameplay system", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("weapon system", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("movement system", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("health system", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("fps", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("monobehaviour", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("scriptableobject", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("weapon", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("shooter", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("multiplayer", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeNuancedUnityObjective(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;
        return objective.Contains("cooldown", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("error state", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("compile error", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("inspector", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("raw code", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikePersonalSoftwareObjective(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;
        return objective.Contains("personal", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("productivity", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("profile", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("preferences", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("tasks", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("reminders", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("dashboard", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeUiDemoObjective(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;
        var hasUiIntent = objective.Contains("ui demo", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("simple demo app", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("web ui", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("demo app with a ui", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("interactive demo", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("chat bot interface", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("chatbot interface", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("avalonia", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("maui", StringComparison.OrdinalIgnoreCase);
        var hasDomainKnowledgeIntent = objective.Contains("domain knowledge", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("knowledge layer", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("retained in that layer", StringComparison.OrdinalIgnoreCase);
        var hasHotloadIntent = objective.Contains("request a new feature", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("spin it up", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("dynamically load", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("ui changes", StringComparison.OrdinalIgnoreCase);
        return hasUiIntent || hasDomainKnowledgeIntent || hasHotloadIntent;
    }

    private static bool LooksLikeUiFeatureHotloadObjective(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return false;
        return objective.Contains("UI_FEATURE_HOTLOAD", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("AVALONIA_FEATURE_HOTLOAD", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("AVALONIA_APP_TRANSFORM", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("hot-loadable ui feature module", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildUiFeatureHotloadToolCallsJson(string systemPrompt, string objective)
    {
        var root = ResolveRepoRootFromSystemPrompt(systemPrompt);
        var request = ExtractUiFeatureRequest(objective);
        var slug = SlugifyIdentifier(request);
        var retainedCapabilities = MatchUiDomainCapabilities(request);

        if (objective.Contains("AVALONIA_FEATURE_HOTLOAD", StringComparison.OrdinalIgnoreCase))
        {
            var descriptorPath = $"docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/GeneratedExtensions/{slug}.json";
            var descriptorContent = BuildAvaloniaFeatureDescriptorJson(request, slug, retainedCapabilities);
            return JsonSerializer.Serialize(new
            {
                tool_calls = new[] { CreateWriteCall(root, descriptorPath, descriptorContent) }
            });
        }
        if (objective.Contains("AVALONIA_APP_TRANSFORM", StringComparison.OrdinalIgnoreCase))
        {
            var descriptorPath = $"docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/GeneratedExtensions/{slug}.json";
            var descriptorContent = BuildAvaloniaAppTransformDescriptorJson(request, slug, retainedCapabilities);
            return JsonSerializer.Serialize(new
            {
                tool_calls = new[] { CreateWriteCall(root, descriptorPath, descriptorContent) }
            });
        }

        var modulePath = $"docs/UiDomainDemoGenerated/app/generated/{slug}.js";
        var moduleSource = BuildUiFeatureModuleSource(request, slug, retainedCapabilities);

        var calls = new List<object>
        {
            CreateWriteCall(root, modulePath, moduleSource)
        };

        return JsonSerializer.Serialize(new { tool_calls = calls });
    }

    private static string ExtractUiFeatureRequest(string objective)
    {
        var match = Regex.Match(objective, @"Feature request:\s*(?<req>.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (match.Success && !string.IsNullOrWhiteSpace(match.Groups["req"].Value))
        {
            var raw = match.Groups["req"].Value.Trim();
            raw = raw.Replace("\\n", " ", StringComparison.Ordinal);
            var markerIndex = raw.IndexOf("Write output module under", StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
                markerIndex = raw.IndexOf("Write output descriptor under", StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
                raw = raw[..markerIndex].Trim();
            raw = raw.Trim().TrimEnd('.', ';');
            if (!string.IsNullOrWhiteSpace(raw))
                return raw;
        }
        return "Generated feature module";
    }

    private static string SlugifyIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "feature_generated";
        var normalized = value.ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9]+", "_");
        normalized = normalized.Trim('_');
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "feature_generated";
        if (char.IsDigit(normalized[0]))
            normalized = $"f_{normalized}";
        return normalized;
    }

    private static string[] MatchUiDomainCapabilities(string requestText)
    {
        var request = requestText.ToLowerInvariant();
        var matches = new List<string>();
        var catalog = new (string Id, string Token)[]
        {
            ("quest-tracking", "quest"),
            ("inventory-events", "inventory"),
            ("ability-cooldowns", "ability"),
            ("onboarding-flows", "onboarding"),
            ("ui-notifications", "notification")
        };
        foreach (var item in catalog)
        {
            if (request.Contains(item.Token, StringComparison.Ordinal))
                matches.Add(item.Id);
        }
        if (matches.Count == 0)
            matches.AddRange(new[] { "quest-tracking", "onboarding-flows" });
        return matches.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string BuildUiDemoToolCallsJson(string systemPrompt)
    {
        var root = ResolveRepoRootFromSystemPrompt(systemPrompt);
        var extensionCommands = new List<(string ClassName, string CommandName, string ExtensionId, string[] Dependencies)>
        {
            ("DomainKnowledgeExtensionCommand", "ext-domain-knowledge", "domain-knowledge", Array.Empty<string>()),
            ("UiShellExtensionCommand", "ext-ui-shell", "ui-shell", new[] { "domain-knowledge" }),
            ("UiWorkflowExtensionCommand", "ext-ui-workflow", "ui-workflow", new[] { "domain-knowledge", "ui-shell" }),
            ("FeatureHotloadExtensionCommand", "ext-feature-hotload", "feature-hotload", new[] { "domain-knowledge", "ui-shell", "ui-workflow" }),
        };

        var calls = new List<object>
        {
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/README.md", BuildUiDemoReadmeSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/app/index.html", BuildUiDemoHtmlSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/app/styles.css", BuildUiDemoCssSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/app/app.js", BuildUiDemoJsSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/app/domain-knowledge.json", BuildUiDomainKnowledgeJsonSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/host/UiDemoHost.csproj", BuildUiDemoHostProjectSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/host/Program.cs", BuildUiDemoHostProgramSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/host/UiDemoSmoke.csproj", BuildUiDemoSmokeProjectSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/host/SmokeProgram.cs", BuildUiDemoSmokeProgramSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.Abstractions/Nexo.Ui.Abstractions.csproj", BuildAvaloniaAbstractionsProjectSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.Abstractions/UiContracts.cs", BuildAvaloniaUiContractsSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/Nexo.Ui.AvaloniaHost.csproj", BuildAvaloniaHostProjectSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/Program.cs", BuildAvaloniaHostProgramSource()),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/GeneratedExtensions/.gitkeep", string.Empty),
            CreateWriteCall(root, "docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/README.md", BuildAvaloniaHostReadmeSource()),

            // Command-structure scaffolding for composable extension commands.
            CreateWriteCall(root, "src/Nexo.CLI/Commands/SelfExtendGenerated/IComposableExtensionCommand.cs", BuildComposableCommandContractSource()),
        };

        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"src/Nexo.CLI/Commands/SelfExtendGenerated/{ext.ClassName}.cs",
                BuildExtensionCommandSource(ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }

        calls.Add(CreateWriteCall(
            root,
            "src/Nexo.CLI/Commands/SelfExtendGenerated/SelfExtendUiDemoBundleCommand.cs",
            BuildBundleCommandSource(
                bundleClassName: "SelfExtendUiDemoBundleCommand",
                bundleCommandName: "self-extend-ui-demo-bundle",
                extensionCommands: extensionCommands.Select(e => (e.ClassName, e.CommandName)).ToArray())));

        // Generated tests that validate extension command structure.
        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/{ext.ClassName}StructureTests.cs",
                BuildExtensionCommandStructureTestSource($"{ext.ClassName}StructureTests", ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }

        calls.Add(CreateWriteCall(
            root,
            "src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/SelfExtendUiDemoBundleCommandStructureTests.cs",
            BuildBundleCommandStructureTestSource(
                testClassName: "SelfExtendUiDemoBundleCommandStructureTests",
                bundleClassName: "SelfExtendUiDemoBundleCommand",
                expectedBundleCommandName: "self-extend-ui-demo-bundle",
                expectedCommands: extensionCommands.Select(e => e.CommandName).ToArray())));

        calls.Add(CreateWriteCall(
            root,
            "src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/UiDomainKnowledgeRetentionTests.cs",
            LoadCanonicalGeneratedSource(
                root,
                "src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/UiDomainKnowledgeRetentionTests.cs")));

        return JsonSerializer.Serialize(new { tool_calls = calls });
    }

    private static string BuildPersonalAppToolCallsJson(string systemPrompt)
    {
        var root = ResolveRepoRootFromSystemPrompt(systemPrompt);
        var extensionCommands = new List<(string ClassName, string CommandName, string ExtensionId, string[] Dependencies)>
        {
            ("ProfileExtensionCommand", "ext-profile", "profile", Array.Empty<string>()),
            ("PreferencesExtensionCommand", "ext-preferences", "preferences", new[] { "profile" }),
            ("TasksExtensionCommand", "ext-tasks", "tasks", new[] { "profile", "preferences" }),
            ("RemindersExtensionCommand", "ext-reminders", "reminders", new[] { "tasks" }),
            ("DashboardExtensionCommand", "ext-dashboard", "dashboard", new[] { "tasks", "reminders" }),
        };

        var calls = new List<object>
        {
            CreateWriteCall(root, "docs/PersonalAppGenerated/UserProfile.cs", BuildPersonalUserProfileSource()),
            CreateWriteCall(root, "docs/PersonalAppGenerated/UserPreferences.cs", BuildPersonalUserPreferencesSource()),
            CreateWriteCall(root, "docs/PersonalAppGenerated/PersonalTaskItem.cs", BuildPersonalTaskItemSource()),
            CreateWriteCall(root, "docs/PersonalAppGenerated/PersonalReminder.cs", BuildPersonalReminderSource()),
            CreateWriteCall(root, "docs/PersonalAppGenerated/ProgressDashboard.cs", BuildPersonalProgressDashboardSource()),
            CreateWriteCall(root, "docs/PersonalAppGenerated/README.md", BuildPersonalAppReadmeSource()),

            // Command-structure scaffolding for composable extension commands.
            CreateWriteCall(root, "src/Nexo.CLI/Commands/SelfExtendGenerated/IComposableExtensionCommand.cs", BuildComposableCommandContractSource()),
        };

        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"src/Nexo.CLI/Commands/SelfExtendGenerated/{ext.ClassName}.cs",
                BuildExtensionCommandSource(ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }

        calls.Add(CreateWriteCall(
            root,
            "src/Nexo.CLI/Commands/SelfExtendGenerated/SelfExtendPersonalBundleCommand.cs",
            BuildBundleCommandSource(
                bundleClassName: "SelfExtendPersonalBundleCommand",
                bundleCommandName: "self-extend-personal-bundle",
                extensionCommands: extensionCommands.Select(e => (e.ClassName, e.CommandName)).ToArray())));

        // Generated tests that validate extension command structure.
        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/{ext.ClassName}StructureTests.cs",
                BuildExtensionCommandStructureTestSource($"{ext.ClassName}StructureTests", ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }

        calls.Add(CreateWriteCall(
            root,
            "src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/SelfExtendPersonalBundleCommandStructureTests.cs",
            BuildBundleCommandStructureTestSource(
                testClassName: "SelfExtendPersonalBundleCommandStructureTests",
                bundleClassName: "SelfExtendPersonalBundleCommand",
                expectedBundleCommandName: "self-extend-personal-bundle",
                extensionCommands.Select(e => e.CommandName).ToArray())));

        return JsonSerializer.Serialize(new { tool_calls = calls });
    }

    private static string BuildUnityBootstrapToolCallsJson(string systemPrompt, string objective)
    {
        var root = ResolveRepoRootFromSystemPrompt(systemPrompt);
        var nuanced = LooksLikeNuancedUnityObjective(objective);
        var includeJump = objective.Contains("jump", StringComparison.OrdinalIgnoreCase);
        var includeSprint = objective.Contains("sprint", StringComparison.OrdinalIgnoreCase);
        var includeRegistry = objective.Contains("registry", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("compose", StringComparison.OrdinalIgnoreCase)
            || objective.Contains("composed", StringComparison.OrdinalIgnoreCase);

        var interfaceContent = """
namespace Nexo.Unity.Generated;

public interface IGeneratedGameplaySystem
{
    string Id { get; }
    string DisplayName { get; }
    void Tick(SystemContext context);
}
""";

        var contextContent = BuildSystemContextSource(includeJump, includeSprint);
        var dashContent = nuanced ? BuildDashSystemNuancedSource() : BuildDashSystemBaselineSource();
        var jumpContent = BuildJumpSystemSource();
        var sprintContent = BuildSprintSystemSource();
        var registryContent = BuildAbilityRegistrySource();
        var errorStateContent = BuildErrorStateSource();
        var inspectorSnapshotContent = BuildInspectorSnapshotSource();

        var extensionCommands = new List<(string ClassName, string CommandName, string ExtensionId, string[] Dependencies)>
        {
            ("DashExtensionCommand", "ext-dash", "dash", Array.Empty<string>()),
            ("JumpExtensionCommand", "ext-jump", "jump", new[] { "dash" }),
            ("SprintExtensionCommand", "ext-sprint", "sprint", new[] { "dash" }),
            ("AbilityRegistryExtensionCommand", "ext-registry", "registry", new[] { "dash", "jump", "sprint" }),
        };

        var calls = new List<object>
        {
            CreateWriteCall(root, "Assets/Scripts/Generated/IGeneratedGameplaySystem.cs", interfaceContent),
            CreateWriteCall(root, "Assets/Scripts/Generated/SystemContext.cs", contextContent),
            CreateWriteCall(root, "Assets/Scripts/Generated/DashAbilitySystem.cs", dashContent),
        };

        if (includeJump)
            calls.Add(CreateWriteCall(root, "Assets/Scripts/Generated/JumpAbilitySystem.cs", jumpContent));
        if (includeSprint)
            calls.Add(CreateWriteCall(root, "Assets/Scripts/Generated/SprintAbilitySystem.cs", sprintContent));
        if (includeRegistry)
            calls.Add(CreateWriteCall(root, "Assets/Scripts/Generated/AbilityRegistry.cs", registryContent));

        if (nuanced)
        {
            calls.Add(CreateWriteCall(root, "Assets/Scripts/Generated/GeneratedSystemErrorState.cs", errorStateContent));
            calls.Add(CreateWriteCall(root, "Assets/Scripts/Generated/GeneratedSystemInspectorSnapshot.cs", inspectorSnapshotContent));
        }

        // Command-structure scaffolding for composition.
        calls.Add(CreateWriteCall(root, "src/Nexo.CLI/Commands/SelfExtendGenerated/IComposableExtensionCommand.cs", BuildComposableCommandContractSource()));
        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"src/Nexo.CLI/Commands/SelfExtendGenerated/{ext.ClassName}.cs",
                BuildExtensionCommandSource(ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }
        calls.Add(CreateWriteCall(
            root,
            "src/Nexo.CLI/Commands/SelfExtendGenerated/SelfExtendBundleCommand.cs",
            BuildBundleCommandSource(extensionCommands.Select(e => (e.ClassName, e.CommandName)).ToArray())));

        // Generated tests that validate extension command structure.
        foreach (var ext in extensionCommands)
        {
            calls.Add(CreateWriteCall(
                root,
                $"src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/{ext.ClassName}StructureTests.cs",
                BuildExtensionCommandStructureTestSource($"{ext.ClassName}StructureTests", ext.ClassName, ext.CommandName, ext.ExtensionId, ext.Dependencies)));
        }
        calls.Add(CreateWriteCall(
            root,
            "src/Nexo.Tests.CLI/Tests/Commands/SelfExtendGenerated/SelfExtendBundleCommandStructureTests.cs",
            BuildBundleCommandStructureTestSource(
                "SelfExtendBundleCommandStructureTests",
                extensionCommands.Select(e => e.CommandName).ToArray())));

        return JsonSerializer.Serialize(new { tool_calls = calls });
    }

    private static object CreateWriteCall(string root, string path, string content) => new
    {
        id = "repo.fs.write",
        arguments = new
        {
            root,
            path,
            content
        }
    };

    private static string BuildSystemContextSource(bool includeJump, bool includeSprint)
    {
        var jumpFields = includeJump
            ? """
    public bool JumpPressed { get; init; }
    public float JumpForce { get; init; } = 8f;
"""
            : string.Empty;
        var sprintFields = includeSprint
            ? """
    public bool SprintPressed { get; init; }
    public float SprintSpeedMultiplier { get; init; } = 1.5f;
"""
            : string.Empty;

        return $$"""
namespace Nexo.Unity.Generated;

public sealed class SystemContext
{
    public float DeltaTime { get; init; }
    public float CurrentTimeSeconds { get; init; }
    public bool DashPressed { get; init; }
    public float DashSpeed { get; init; } = 12f;
    public float DashDurationSeconds { get; init; } = 0.2f;
    public float DashCooldownSeconds { get; init; } = 1.0f;
{{jumpFields}}{{sprintFields}}
}
""";
    }

    private static string BuildDashSystemBaselineSource() => """
namespace Nexo.Unity.Generated;

public sealed class DashAbilitySystem : IGeneratedGameplaySystem
{
    private float _remainingDashSeconds;
    private float _cooldownEndsAtSeconds;

    public string Id => "dash-ability";
    public string DisplayName => "Dash Ability";

    public void Tick(SystemContext context)
    {
        if (context.DashPressed && context.CurrentTimeSeconds >= _cooldownEndsAtSeconds)
        {
            _remainingDashSeconds = context.DashDurationSeconds;
            _cooldownEndsAtSeconds = context.CurrentTimeSeconds + context.DashCooldownSeconds;
        }

        if (_remainingDashSeconds > 0f)
            _remainingDashSeconds -= context.DeltaTime;
    }
}
""";

    private static string BuildDashSystemNuancedSource() => """
namespace Nexo.Unity.Generated;

public sealed class DashAbilitySystem : IGeneratedGameplaySystem
{
    private float _remainingDashSeconds;
    private float _cooldownEndsAtSeconds;
    private GeneratedSystemErrorState _errorState = GeneratedSystemErrorState.None;

    public string Id => "dash-ability";
    public string DisplayName => "Dash Ability";

    public void Tick(SystemContext context)
    {
        if (context.DashPressed && context.CurrentTimeSeconds >= _cooldownEndsAtSeconds)
        {
            _remainingDashSeconds = context.DashDurationSeconds;
            _cooldownEndsAtSeconds = context.CurrentTimeSeconds + context.DashCooldownSeconds;
        }

        if (_remainingDashSeconds > 0f)
            _remainingDashSeconds -= context.DeltaTime;
    }

    public GeneratedSystemInspectorSnapshot Inspect(string generatedCode) =>
        new(
            SystemId: Id,
            RawGeneratedCode: generatedCode,
            ErrorState: _errorState,
            GeneratedAtUtc: System.DateTimeOffset.UtcNow);
}
""";

    private static string BuildJumpSystemSource() => """
namespace Nexo.Unity.Generated;

public sealed class JumpAbilitySystem : IGeneratedGameplaySystem
{
    private bool _airborne;
    private float _verticalVelocity;

    public string Id => "jump-ability";
    public string DisplayName => "Jump Ability";

    public void Tick(SystemContext context)
    {
        if (context.JumpPressed && !_airborne)
        {
            _airborne = true;
            _verticalVelocity = context.JumpForce;
        }

        if (_airborne)
        {
            _verticalVelocity -= 9.8f * context.DeltaTime;
            if (_verticalVelocity <= 0f)
                _airborne = false;
        }
    }
}
""";

    private static string BuildSprintSystemSource() => """
namespace Nexo.Unity.Generated;

public sealed class SprintAbilitySystem : IGeneratedGameplaySystem
{
    public string Id => "sprint-ability";
    public string DisplayName => "Sprint Ability";
    public float CurrentSpeedMultiplier { get; private set; } = 1f;

    public void Tick(SystemContext context)
    {
        CurrentSpeedMultiplier = context.SprintPressed ? context.SprintSpeedMultiplier : 1f;
    }
}
""";

    private static string BuildAbilityRegistrySource() => """
namespace Nexo.Unity.Generated;

public sealed class AbilityRegistry
{
    private readonly Dictionary<string, IGeneratedGameplaySystem> _systems = new(StringComparer.Ordinal);

    public AbilityRegistry(IEnumerable<IGeneratedGameplaySystem> systems)
    {
        foreach (var system in systems)
            _systems[system.Id] = system;
    }

    public IReadOnlyCollection<IGeneratedGameplaySystem> All => _systems.Values;

    public bool TryGet(string id, out IGeneratedGameplaySystem? system)
        => _systems.TryGetValue(id, out system);
}
""";

    private static string BuildPersonalUserProfileSource() => """
namespace Nexo.Personal.Generated;

public sealed record UserProfile(
    string UserId,
    string DisplayName,
    string TimeZoneId,
    string Locale);
""";

    private static string BuildPersonalUserPreferencesSource() => """
namespace Nexo.Personal.Generated;

public sealed record UserPreferences(
    bool StartWithTodayView,
    bool EnableReminderNotifications,
    int DefaultFocusMinutes,
    int DailyGoalPoints);
""";

    private static string BuildPersonalTaskItemSource() => """
namespace Nexo.Personal.Generated;

public sealed class PersonalTaskItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public bool Completed { get; private set; }
    public int Priority { get; init; } = 3;
    public System.DateTimeOffset? DueAtUtc { get; init; }

    public void MarkCompleted() => Completed = true;
}
""";

    private static string BuildPersonalReminderSource() => """
namespace Nexo.Personal.Generated;

public sealed class PersonalReminder
{
    public required string Id { get; init; }
    public required string TaskId { get; init; }
    public required System.DateTimeOffset FireAtUtc { get; init; }
    public bool Sent { get; private set; }

    public bool ShouldFire(System.DateTimeOffset nowUtc) => !Sent && nowUtc >= FireAtUtc;
    public void MarkSent() => Sent = true;
}
""";

    private static string BuildPersonalProgressDashboardSource() => """
namespace Nexo.Personal.Generated;

public sealed class ProgressDashboard
{
    public static int CalculateDailyPoints(System.Collections.Generic.IEnumerable<PersonalTaskItem> tasks)
    {
        if (tasks is null)
            return 0;

        var points = 0;
        foreach (var task in tasks)
        {
            if (!task.Completed)
                continue;
            points += task.Priority switch
            {
                <= 1 => 5,
                2 => 3,
                _ => 1
            };
        }
        return points;
    }
}
""";

    private static string BuildPersonalAppReadmeSource() => """
# Personal App Scaffold (Generated)

This generated package models a personal productivity app that keeps the Nexo backend infrastructure unchanged.

Artifacts:
- User profile and preference models
- Task and reminder models
- Progress dashboard scoring logic
- Composable CLI extension commands for profile/preferences/tasks/reminders/dashboard
- Generated structure tests for each extension command and the composed bundle
""";

    private static string BuildUiDemoReadmeSource() => """
# UI Demo Scaffold (Generated)

This generated demo provides an interactive browser chatbot with a retained domain-knowledge layer.

Outputs:
- `docs/UiDomainDemoGenerated/app/index.html` chat + feature studio UI shell
- `docs/UiDomainDemoGenerated/app/app.js` chatbot workflow + real scaffold/hot-load runtime through server API
- `docs/UiDomainDemoGenerated/app/domain-knowledge.json` retained domain knowledge catalog
- `docs/UiDomainDemoGenerated/host/Program.cs` .NET API/static host that invokes `nexo self-extend` for feature scaffolding
- `docs/UiDomainDemoGenerated/host/SmokeProgram.cs` .NET smoke checker for host + UI wiring
- `docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.Abstractions` framework-neutral UI contracts (cross-framework abstraction layer)
- `docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost` Linux-compatible Avalonia desktop host with dynamic extension loading
- composable extension commands + generated structure tests
""";

    private static string BuildUiDemoHtmlSource() => """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Nexo UI Demo</title>
  <link rel="stylesheet" href="./styles.css" />
</head>
<body>
  <main class="app-shell">
    <header>
      <h1>Nexo Interactive Domain Demo</h1>
      <p id="status-line">Loading domain knowledge...</p>
    </header>

    <section class="panel">
      <h2>Nexo Chatbot</h2>
      <div id="chat-log" class="chat-log"></div>
      <div class="row">
        <input id="chat-input" type="text" value="What is Nexo?" />
        <button id="chat-send-btn" type="button">Send</button>
      </div>
    </section>

    <section class="panel">
      <h2>Retained domain knowledge</h2>
      <ul id="knowledge-list"></ul>
    </section>

    <section class="panel">
      <h2>Feature request studio</h2>
      <label for="feature-input">Request new feature</label>
      <div class="row">
        <input id="feature-input" type="text" value="Add a quest streak tracker widget for returning players" />
        <button id="scaffold-feature-btn" type="button">Scaffold + Hot-load</button>
      </div>
      <p id="feature-status" class="muted">Awaiting feature request.</p>
    </section>

    <section class="panel">
      <h2>Dynamically loaded features</h2>
      <div id="dynamic-feature-host" class="dynamic-feature-host"></div>
    </section>

    <section class="panel">
      <h2>Generated scaffold plan</h2>
      <pre id="output-pane"></pre>
    </section>
  </main>
  <script src="./app.js" defer></script>
</body>
</html>
""";

    private static string BuildUiDemoCssSource() => """
:root {
  color-scheme: dark;
  font-family: Arial, sans-serif;
}

body {
  margin: 0;
  background: #111827;
  color: #e5e7eb;
}

.app-shell {
  max-width: 920px;
  margin: 0 auto;
  padding: 24px;
}

.panel {
  background: #1f2937;
  border: 1px solid #374151;
  border-radius: 10px;
  padding: 14px;
  margin-top: 14px;
}

.row {
  display: flex;
  gap: 8px;
  align-items: center;
}

#chat-input,
#feature-input {
  flex: 1;
  margin-top: 8px;
  margin-bottom: 8px;
  padding: 10px;
  border-radius: 6px;
  border: 1px solid #4b5563;
  background: #111827;
  color: #e5e7eb;
}

#chat-send-btn,
#scaffold-feature-btn {
  padding: 8px 12px;
  border-radius: 6px;
  border: 1px solid #4b5563;
  background: #2563eb;
  color: #ffffff;
  cursor: pointer;
  white-space: nowrap;
}

#knowledge-list li {
  margin-bottom: 6px;
}

.chat-log {
  max-height: 220px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  gap: 8px;
  background: #111827;
  border-radius: 6px;
  padding: 10px;
}

.chat-msg {
  padding: 8px 10px;
  border-radius: 8px;
  border: 1px solid #374151;
  white-space: pre-wrap;
}

.chat-user {
  background: #1e3a8a;
}

.chat-assistant {
  background: #0f766e;
}

.dynamic-feature-host {
  display: grid;
  gap: 10px;
}

.feature-card {
  border: 1px solid #4b5563;
  border-radius: 8px;
  padding: 10px;
  background: #111827;
}

.feature-chip-row {
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
  margin-top: 8px;
}

.feature-chip {
  font-size: 12px;
  border: 1px solid #334155;
  border-radius: 999px;
  padding: 2px 8px;
}

.muted {
  color: #9ca3af;
}

#output-pane {
  background: #111827;
  border-radius: 6px;
  padding: 10px;
  min-height: 120px;
  white-space: pre-wrap;
}
""";

    private static string BuildUiDemoJsSource() => """
const statusLine = document.getElementById("status-line");
const knowledgeList = document.getElementById("knowledge-list");
const chatLog = document.getElementById("chat-log");
const chatInput = document.getElementById("chat-input");
const chatSendButton = document.getElementById("chat-send-btn");
const featureInput = document.getElementById("feature-input");
const scaffoldFeatureButton = document.getElementById("scaffold-feature-btn");
const featureStatus = document.getElementById("feature-status");
const dynamicFeatureHost = document.getElementById("dynamic-feature-host");
const outputPane = document.getElementById("output-pane");

let domainCatalog = [];
const dynamicFeatures = [];

function appendChatMessage(role, text) {
  const item = document.createElement("div");
  item.className = `chat-msg ${role === "user" ? "chat-user" : "chat-assistant"}`;
  item.textContent = text;
  chatLog.appendChild(item);
  chatLog.scrollTop = chatLog.scrollHeight;
}

async function loadDomainKnowledge() {
  const response = await fetch("./domain-knowledge.json");
  if (!response.ok) {
    throw new Error("Failed to load domain knowledge");
  }
  const data = await response.json();
  domainCatalog = data.capabilities ?? [];
  renderKnowledge();
  statusLine.textContent = `Domain knowledge ready: ${domainCatalog.length} capabilities loaded`;
  appendChatMessage("assistant", "Hi! I am the Nexo demo assistant. Ask what Nexo is, or request a feature to scaffold and hot-load.");
}

function renderKnowledge() {
  knowledgeList.innerHTML = "";
  for (const item of domainCatalog) {
    const li = document.createElement("li");
    li.textContent = `${item.id}: ${item.summary}`;
    knowledgeList.appendChild(li);
  }
}

function buildWorkflowDraft(requestText, selectedCapabilities) {
  return {
    request: requestText,
    retainedDomainKnowledge: selectedCapabilities,
    suggestedSteps: [
      "Analyze request against retained capability catalog",
      "Synthesize scaffolding plan and compose extension commands",
      "Hot-load generated feature into UI shell",
      "Run SelfExtendGenerated tests and UI smoke test"
    ]
  };
}

function explainNexo(questionText) {
  const q = questionText.toLowerCase();
  if (q.includes("what is nexo")) {
    return "Nexo is an orchestration platform that composes domain capabilities, scaffolds features, and validates changes with built-in tests.";
  }
  if (q.includes("how") && q.includes("feature")) {
    return "In this demo, Nexo maps your request to retained domain knowledge, composes scaffold commands, then hot-loads a new feature card into the UI.";
  }
  return "I can explain Nexo or scaffold a feature. Try: 'What is Nexo?' or 'Add feature: daily quest hints'.";
}

async function scaffoldFeatureHotload(featureRequest, source) {
  featureStatus.textContent = "Scaffolding feature through nexo self-extend...";
  const response = await fetch("/api/scaffold-feature", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ featureRequest })
  });
  if (!response.ok) {
    const message = await response.text();
    throw new Error(`Scaffold request failed: ${message}`);
  }

  const payload = await response.json();
  const moduleUrl = `${payload.moduleUrl}?ts=${Date.now()}`;
  const loaded = await import(moduleUrl);
  if (typeof loaded.mountFeature !== "function") {
    throw new Error("Generated module is missing mountFeature export.");
  }

  const model = {
    featureId: payload.featureId,
    featureRequest: payload.featureRequest,
    summary: payload.summary,
    retainedDomainKnowledge: payload.retainedDomainKnowledge ?? [],
    source
  };
  dynamicFeatures.unshift(model);

  loaded.mountFeature(dynamicFeatureHost, model);

  const draft = buildWorkflowDraft(featureRequest, model.retainedDomainKnowledge);
  outputPane.textContent = JSON.stringify(
    {
      ...draft,
      featureId: model.featureId,
      moduleUrl: payload.moduleUrl
    },
    null,
    2
  );

  featureStatus.textContent = `Feature ${model.featureId} scaffolded and hot-loaded with ${model.retainedDomainKnowledge.length} domain capabilities.`;
  appendChatMessage("assistant", `Feature ready: ${model.featureId}. UI updated live with retained knowledge: ${model.retainedDomainKnowledge.join(", ")}`);
}

chatSendButton.addEventListener("click", () => {
  const text = chatInput.value.trim();
  if (!text) return;
  appendChatMessage("user", text);

  const normalized = text.toLowerCase();
  if (normalized.startsWith("add feature:") || normalized.startsWith("request feature:")) {
    const requestText = text.split(":").slice(1).join(":").trim();
    if (requestText.length > 0) {
      scaffoldFeatureHotload(requestText, "chatbot").catch(error => {
        featureStatus.textContent = `Scaffold failed: ${error.message}`;
        appendChatMessage("assistant", `Feature scaffold failed: ${error.message}`);
      });
    } else {
      appendChatMessage("assistant", "Please include a feature description after ':'");
    }
  } else {
    appendChatMessage("assistant", explainNexo(text));
  }
});

scaffoldFeatureButton.addEventListener("click", () => {
  const requestText = featureInput.value.trim();
  if (!requestText) return;
  scaffoldFeatureHotload(requestText, "feature-studio").catch(error => {
    featureStatus.textContent = `Scaffold failed: ${error.message}`;
    appendChatMessage("assistant", `Feature scaffold failed: ${error.message}`);
  });
});

loadDomainKnowledge().catch(error => {
  statusLine.textContent = `Domain knowledge load failed: ${error.message}`;
  outputPane.textContent = "UI entered degraded mode; no domain capability catalog available.";
  appendChatMessage("assistant", "Domain knowledge failed to load. Feature scaffolding is unavailable.");
});
""";

    private static string BuildUiDomainKnowledgeJsonSource() => """
{
  "capabilities": [
    {
      "id": "quest-tracking",
      "matchToken": "quest",
      "summary": "Tracks player quest state and completion milestones."
    },
    {
      "id": "inventory-events",
      "matchToken": "inventory",
      "summary": "Handles item grants, removals, and inventory notifications."
    },
    {
      "id": "ability-cooldowns",
      "matchToken": "ability",
      "summary": "Applies shared cooldown semantics for gameplay actions."
    },
    {
      "id": "onboarding-flows",
      "matchToken": "onboarding",
      "summary": "Designs first-time user flows and progressive feature unlocks."
    },
    {
      "id": "ui-notifications",
      "matchToken": "notification",
      "summary": "Renders actionable notifications and in-session prompts."
    }
  ]
}
""";

    private static string BuildUiDemoHostProjectSource() => """
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
  </ItemGroup>
</Project>
""";

    private static string BuildUiDemoHostProgramSource() => """
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("UI_DEMO_URLS") ?? "http://127.0.0.1:4173");
var app = builder.Build();

var appRoot = ResolveAppRoot();
var repoRoot = ResolveRepoRoot(appRoot);
var fileProvider = new PhysicalFileProvider(appRoot);

app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = fileProvider
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider
});

app.MapPost("/api/scaffold-feature", async (ScaffoldFeatureRequest request, CancellationToken ct) =>
{
    var featureRequest = request.FeatureRequest?.Trim();
    if (string.IsNullOrWhiteSpace(featureRequest))
        return Results.BadRequest(new { ok = false, error = "featureRequest is required" });

    var run = await RunSelfExtendAsync(repoRoot, featureRequest, ct).ConfigureAwait(false);
    if (run.ExitCode != 0)
    {
        return Results.Json(new
        {
            ok = false,
            error = "self-extend failed",
            stdout = TrimTail(run.StdOut, 2000),
            stderr = TrimTail(run.StdErr, 2000)
        }, statusCode: 500);
    }

    var featureId = Slugify(featureRequest);
    var moduleFile = Path.Combine(appRoot, "generated", $"{featureId}.js");
    if (!File.Exists(moduleFile))
        return Results.Json(new { ok = false, error = $"generated module not found: {moduleFile}" }, statusCode: 500);

    var retained = MapCapabilities(appRoot, featureRequest);
    return Results.Ok(new
    {
        ok = true,
        featureId,
        featureRequest,
        moduleUrl = $"/generated/{featureId}.js",
        retainedDomainKnowledge = retained,
        summary = "Generated by Nexo self-scaffold pipeline and hot-loaded into the active UI shell."
    });
});

app.Run();

static string ResolveAppRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, "app", "index.html");
        if (File.Exists(candidate))
            return Path.Combine(dir.FullName, "app");
        dir = dir.Parent;
    }
    throw new InvalidOperationException("Unable to resolve app root.");
}

static string ResolveRepoRoot(string appRoot)
{
    var dir = new DirectoryInfo(appRoot);
    while (dir != null)
    {
        var hasSrc = Directory.Exists(Path.Combine(dir.FullName, "src"));
        var hasDocs = Directory.Exists(Path.Combine(dir.FullName, "docs"));
        if (hasSrc && hasDocs)
            return dir.FullName;
        dir = dir.Parent;
    }
    throw new InvalidOperationException("Unable to resolve repository root.");
}

static async Task<(int ExitCode, string StdOut, string StdErr)> RunSelfExtendAsync(string repoRoot, string featureRequest, CancellationToken ct)
{
    var goal = $"UI_FEATURE_HOTLOAD Feature request: {featureRequest}. Write output module under docs/UiDomainDemoGenerated/app/generated.";
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        WorkingDirectory = repoRoot,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    psi.ArgumentList.Add("run");
    psi.ArgumentList.Add("--project");
    psi.ArgumentList.Add("src/Nexo.CLI");
    psi.ArgumentList.Add("--");
    psi.ArgumentList.Add("self-extend");
    psi.ArgumentList.Add("run");
    psi.ArgumentList.Add("--goal");
    psi.ArgumentList.Add(goal);
    psi.ArgumentList.Add("--repo-root");
    psi.ArgumentList.Add(repoRoot);
    psi.ArgumentList.Add("--provider");
    psi.ArgumentList.Add("mock-json");
    psi.ArgumentList.Add("--allow-mock");
    psi.ArgumentList.Add("--json");

    using var process = Process.Start(psi);
    if (process == null)
        return (1, string.Empty, "Failed to start dotnet process.");
    var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
    var stderrTask = process.StandardError.ReadToEndAsync(ct);
    await process.WaitForExitAsync(ct).ConfigureAwait(false);
    return (process.ExitCode, await stdoutTask.ConfigureAwait(false), await stderrTask.ConfigureAwait(false));
}

static string[] MapCapabilities(string appRoot, string featureRequest)
{
    var catalogPath = Path.Combine(appRoot, "domain-knowledge.json");
    var text = File.ReadAllText(catalogPath);
    using var doc = JsonDocument.Parse(text);
    var caps = doc.RootElement.GetProperty("capabilities");
    var request = featureRequest.ToLowerInvariant();
    var matched = new List<string>();
    foreach (var cap in caps.EnumerateArray())
    {
        var token = cap.TryGetProperty("matchToken", out var t) ? (t.GetString() ?? "") : "";
        var id = cap.TryGetProperty("id", out var i) ? (i.GetString() ?? "") : "";
        if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(id) && request.Contains(token, StringComparison.Ordinal))
            matched.Add(id);
    }
    if (matched.Count == 0)
        matched.AddRange(new[] { "quest-tracking", "onboarding-flows" });
    return matched.Distinct(StringComparer.Ordinal).ToArray();
}

static string Slugify(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return "feature_generated";
    var lower = value.ToLowerInvariant();
    lower = System.Text.RegularExpressions.Regex.Replace(lower, @"[^a-z0-9]+", "_");
    lower = lower.Trim('_');
    if (string.IsNullOrWhiteSpace(lower))
        lower = "feature_generated";
    if (char.IsDigit(lower[0]))
        lower = $"f_{lower}";
    return lower;
}

static string TrimTail(string text, int maxChars)
{
    if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
        return text;
    return text[^maxChars..];
}

public sealed record ScaffoldFeatureRequest(string FeatureRequest);
""";

    private static string BuildUiDemoSmokeProjectSource() => """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="SmokeProgram.cs" />
  </ItemGroup>
</Project>
""";

    private static string BuildUiDemoSmokeProgramSource() => """
using System.Diagnostics;

var uiRoot = ResolveUiDemoRoot();
var appRoot = Path.Combine(uiRoot, "app");
var hostRoot = Path.Combine(uiRoot, "host");

var html = File.ReadAllText(Path.Combine(appRoot, "index.html"));
var js = File.ReadAllText(Path.Combine(appRoot, "app.js"));
var domainJson = File.ReadAllText(Path.Combine(appRoot, "domain-knowledge.json"));
var hostProgram = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));

Assert(html.Contains("chat-send-btn", StringComparison.Ordinal), "Expected chat send button in HTML");
Assert(html.Contains("scaffold-feature-btn", StringComparison.Ordinal), "Expected feature scaffold button in HTML");
Assert(js.Contains("/api/scaffold-feature", StringComparison.Ordinal), "Expected scaffold API call in JS");
Assert(js.Contains("import(moduleUrl)", StringComparison.Ordinal), "Expected dynamic module import in JS");
Assert(hostProgram.Contains("MapPost(\"/api/scaffold-feature\"", StringComparison.Ordinal), "Expected scaffold endpoint in .NET host");
Assert(hostProgram.Contains("UseStaticFiles", StringComparison.Ordinal), "Expected static file hosting in .NET host");
Assert(domainJson.Contains("\"capabilities\"", StringComparison.Ordinal), "Expected domain capability catalog");

Console.WriteLine("ui_smoke_test: ok");

static string ResolveUiDemoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        var appDir = Path.Combine(dir.FullName, "app");
        var hostDir = Path.Combine(dir.FullName, "host");
        if (Directory.Exists(appDir) && Directory.Exists(hostDir))
            return dir.FullName;
        dir = dir.Parent;
    }
    throw new InvalidOperationException("Unable to resolve UiDomainDemoGenerated root.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        Console.Error.WriteLine(message);
        Environment.ExitCode = 1;
        throw new InvalidOperationException(message);
    }
}
""";

    private static string BuildAvaloniaAbstractionsProjectSource() => """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
""";

    private static string BuildAvaloniaUiContractsSource() => """
namespace Nexo.Ui.Abstractions;

public sealed record UiNode(
    string Kind,
    string Id,
    string Text,
    string? Command = null,
    string? AccentHex = null,
    double? Value = null,
    string? Layout = null,
    IReadOnlyList<UiNode>? Children = null);

public sealed record FeatureDescriptor(
    string FeatureId,
    string Title,
    string[] RetainedDomainKnowledge,
    string WowMessage,
    string SourcePath,
    string ExperienceMode,
    UiNode Root);

public interface IUiFrameworkAdapter<TControl>
{
    TControl Create(UiNode node, Action<string>? commandHandler = null);
}

public interface IFeatureScaffolder
{
    Task<FeatureDescriptor> ScaffoldAsync(string featureRequest, CancellationToken cancellationToken = default);
}

public static class CrossFrameworkCompatibility
{
    public const string ContractNotes =
        "Adapters implement IUiFrameworkAdapter<TControl> per framework (Avalonia, MAUI, etc.) while sharing UiNode contracts.";
}
""";

    private static string BuildAvaloniaHostProjectSource() => """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" />
    <PackageReference Include="Avalonia.Desktop" />
    <PackageReference Include="Avalonia.Themes.Fluent" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Nexo.Ui.Abstractions\Nexo.Ui.Abstractions.csproj" />
  </ItemGroup>
</Project>
""";

    private static string BuildAvaloniaHostProgramSource() => """
using System.Diagnostics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.X11;
using Nexo.Ui.Abstractions;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--smoke", StringComparison.OrdinalIgnoreCase)))
            return RunSmoke();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                RenderingMode = new[] { X11RenderingMode.Software }
            })
            .LogToTrace();

    private static int RunSmoke()
    {
        try
        {
            var repoRoot = ResolveRepoRoot();
            var generatedRoot = ResolveGeneratedRoot(repoRoot);
            Directory.CreateDirectory(generatedRoot);
            var scaffolder = new AvaloniaFeatureScaffolder(repoRoot, generatedRoot);
            var descriptor = scaffolder.ScaffoldAsync("Add onboarding notification sidebar", CancellationToken.None)
                .GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(descriptor.FeatureId))
                throw new InvalidOperationException("Descriptor feature id missing.");
            if (descriptor.RetainedDomainKnowledge.Length == 0)
                throw new InvalidOperationException("Descriptor retained knowledge missing.");
            Console.WriteLine("avalonia_smoke: ok");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"avalonia_smoke: failed: {ex.Message}");
            return 1;
        }
    }

    private static string ResolveRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src")) &&
                Directory.Exists(Path.Combine(dir.FullName, "docs")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Unable to resolve repository root.");
    }

    private static string ResolveGeneratedRoot(string repoRoot)
        => Path.Combine(repoRoot, "docs", "UiDomainDemoGenerated", "avalonia", "Nexo.Ui.AvaloniaHost", "GeneratedExtensions");
}

public sealed class App : Application
{
    public App()
    {
        if (!Styles.Any(s => s is FluentTheme))
            Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var repoRoot = ResolveRepoRoot();
            var generatedRoot = Path.Combine(repoRoot, "docs", "UiDomainDemoGenerated", "avalonia", "Nexo.Ui.AvaloniaHost", "GeneratedExtensions");
            Directory.CreateDirectory(generatedRoot);
            var adapter = new AvaloniaUiFrameworkAdapter();
            var scaffolder = new AvaloniaFeatureScaffolder(repoRoot, generatedRoot);
            desktop.MainWindow = new MainWindow(adapter, scaffolder);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string ResolveRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src")) &&
                Directory.Exists(Path.Combine(dir.FullName, "docs")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Unable to resolve repository root.");
    }
}

public sealed class MainWindow : Window
{
    private readonly IUiFrameworkAdapter<Control> _adapter;
    private readonly IFeatureScaffolder _scaffolder;
    private readonly StackPanel _dynamicHost = new() { Spacing = 8 };
    private readonly StackPanel _chatLog = new() { Spacing = 4 };
    private readonly TextBox _chatInput = new() { Text = "What is Nexo?" };
    private readonly TextBox _featureInput = new() { Text = "Add onboarding notification sidebar" };
    private readonly TextBlock _status = new() { Text = "Ready." };
    private readonly TextBox _plan = new() { AcceptsReturn = true, IsReadOnly = true, Height = 180 };
    private readonly TextBox _transformLog = new() { IsReadOnly = true, AcceptsReturn = true, Height = 120, Text = "Awaiting transformed app actions..." };
    private readonly TextBlock _transformPhase = new() { Text = "Phase: Mission shell idle.", Foreground = Brushes.LightGreen };
    private readonly Control _baselineShell;

    public MainWindow(IUiFrameworkAdapter<Control> adapter, IFeatureScaffolder scaffolder)
    {
        _adapter = adapter;
        _scaffolder = scaffolder;
        Title = "Nexo Avalonia Dynamic Extension Demo";
        Width = 1080;
        Height = 760;
        _baselineShell = BuildContent();
        Content = _baselineShell;
        AppendChat("assistant", "Hi! Ask what Nexo is or scaffold a feature.");
    }

    private Control BuildContent()
    {
        var sendChat = new Button { Content = "Send" };
        sendChat.Click += (_, _) => AppendChat("assistant", ExplainNexo(_chatInput.Text ?? string.Empty), includeUserInput: true);

        var scaffold = new Button { Content = "Scaffold + Hot-load" };
        scaffold.Click += async (_, _) => await ScaffoldAsync().ConfigureAwait(false);

        var root = new StackPanel { Spacing = 10, Margin = new Thickness(14) };
        root.Children.Add(new TextBlock
        {
            Text = "Nexo Avalonia Cross-Framework UI Demo",
            FontSize = 22,
            FontWeight = FontWeight.Bold
        });
        root.Children.Add(_status);

        root.Children.Add(new Border
        {
            BorderBrush = Brushes.DimGray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Chatbot" },
                    _chatLog,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children = { _chatInput, sendChat }
                    }
                }
            }
        });

        root.Children.Add(new Border
        {
            BorderBrush = Brushes.DimGray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Feature Scaffolder" },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children = { _featureInput, scaffold }
                    }
                }
            }
        });

        root.Children.Add(new TextBlock { Text = "Dynamically Extended UI (Avalonia Adapter)" });
        root.Children.Add(_dynamicHost);
        root.Children.Add(new TextBlock { Text = "Scaffold Plan" });
        root.Children.Add(_plan);

        return new ScrollViewer { Content = root };
    }

    private async Task ScaffoldAsync()
    {
        var request = (_featureInput.Text ?? string.Empty).Trim();
        if (request.Length == 0)
            return;

        try
        {
            _status.Text = "Scaffolding feature through nexo self-extend...";
            var descriptor = await _scaffolder.ScaffoldAsync(request).ConfigureAwait(true);
            if (string.Equals(descriptor.ExperienceMode, "transform", StringComparison.OrdinalIgnoreCase))
            {
                ApplyAppTransformation(descriptor);
                _status.Text = $"Application transformed via {descriptor.FeatureId}.";
                AppendChat("assistant", $"Full app transformed into {descriptor.Title}. Use Return to Nexo shell to restore.");
            }
            else
            {
                var control = _adapter.Create(descriptor.Root, HandleCommand);
                _dynamicHost.Children.Insert(0, control);
                _status.Text = $"Feature {descriptor.FeatureId} loaded with {descriptor.RetainedDomainKnowledge.Length} domain tags.";
                AppendChat("assistant", $"Feature ready: {descriptor.FeatureId}. Knowledge: {string.Join(", ", descriptor.RetainedDomainKnowledge)}");
            }
            _plan.Text = JsonSerializer.Serialize(new
            {
                request,
                featureId = descriptor.FeatureId,
                retainedDomainKnowledge = descriptor.RetainedDomainKnowledge,
                descriptor.SourcePath,
                wow = descriptor.WowMessage,
                mode = descriptor.ExperienceMode
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _status.Text = $"Scaffold failed: {ex.Message}";
            AppendChat("assistant", $"Feature scaffold failed: {ex.Message}");
        }
    }

    private void AppendChat(string role, string message, bool includeUserInput = false)
    {
        if (includeUserInput)
            _chatLog.Children.Add(new TextBlock { Text = $"user: {_chatInput.Text}" });
        _chatLog.Children.Add(new TextBlock { Text = $"{role}: {message}" });
    }

    private void ApplyAppTransformation(FeatureDescriptor descriptor)
    {
        Title = $"Galactic Operations Console :: {descriptor.Title}";
        _transformPhase.Text = "Phase: Mission shell booted from scaffold.";
        _transformLog.Text = $"Transformed with descriptor: {descriptor.FeatureId}{Environment.NewLine}{descriptor.WowMessage}";

        var generatedExperience = _adapter.Create(descriptor.Root, HandleCommand);
        var wow = new Button
        {
            Content = "Trigger wow sequence",
            Background = Brushes.Cyan,
            Foreground = Brushes.Black,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(12, 8)
        };
        wow.Click += (_, _) => HandleCommand($"feature:{descriptor.FeatureId}:wow");
        var restore = new Button
        {
            Content = "Restore Nexo Shell",
            Background = Brushes.Orange,
            Foreground = Brushes.Black,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(12, 8)
        };
        restore.Click += (_, _) => RestoreNexoShell();
        var deploy = new Button
        {
            Content = "Deploy autonomous patch",
            Background = Brushes.LawnGreen,
            Foreground = Brushes.Black,
            FontWeight = FontWeight.Bold,
            Padding = new Thickness(12, 8)
        };
        deploy.Click += (_, _) => HandleCommand($"feature:{descriptor.FeatureId}:deploy");

        var appGrid = new Grid();
        appGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        appGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        appGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(240)));
        appGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));

        var headerActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
            Children = { wow, deploy, restore }
        };
        DockPanel.SetDock(headerActions, Dock.Right);

        var header = new Border
        {
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(14, 12),
            Child = new DockPanel
            {
                LastChildFill = false,
                Children =
                {
                    new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Galactic Operations Console",
                                FontSize = 24,
                                FontWeight = FontWeight.Bold,
                                Foreground = Brushes.White
                            },
                            new TextBlock
                            {
                                Text = descriptor.Title,
                                Foreground = Brushes.LightGray
                            },
                            new TextBlock
                            {
                                Text = descriptor.WowMessage,
                                Foreground = Brushes.DeepSkyBlue
                            },
                            _transformPhase
                        }
                    },
                    headerActions
                }
            }
        };
        Grid.SetRow(header, 0);
        Grid.SetColumn(header, 0);
        Grid.SetColumnSpan(header, 2);
        appGrid.Children.Add(header);

        var navRail = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#141d32")),
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "Mission Nav", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Brushes.White },
                    CreateTransformNavButton("Overview", "nav:overview"),
                    CreateTransformNavButton("Fleet Grid", "nav:fleet"),
                    CreateTransformNavButton("Signal Radar", "nav:signals"),
                    CreateTransformNavButton("Workflow Graph", "nav:workflow"),
                    CreateTransformNavButton("Threat Intel", "nav:intel")
                }
            }
        };
        Grid.SetRow(navRail, 1);
        Grid.SetColumn(navRail, 0);
        appGrid.Children.Add(navRail);

        var workspace = new Grid { Margin = new Thickness(12) };
        workspace.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        workspace.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        workspace.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var metrics = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children =
            {
                CreateMetricCard("Active fleets", "12", Brushes.DeepSkyBlue),
                CreateMetricCard("Mission sync", "99.2%", Brushes.LawnGreen),
                CreateMetricCard("Latency window", "24ms", Brushes.Gold),
                CreateMetricCard("Risk score", "LOW", Brushes.HotPink)
            }
        };
        Grid.SetRow(metrics, 0);
        workspace.Children.Add(metrics);

        var missionBoard = new Grid { Margin = new Thickness(0, 10, 0, 10) };
        missionBoard.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3, GridUnitType.Star)));
        missionBoard.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2, GridUnitType.Star)));

        var generatedPanel = new Border
        {
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Scaffolded Application Module",
                        FontSize = 18,
                        FontWeight = FontWeight.Bold,
                        Foreground = Brushes.White
                    },
                    generatedExperience
                }
            }
        };
        Grid.SetColumn(generatedPanel, 0);
        missionBoard.Children.Add(generatedPanel);

        var incidentFeed = new Border
        {
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Live Incident Queue",
                        FontSize = 18,
                        FontWeight = FontWeight.Bold,
                        Foreground = Brushes.White
                    },
                    new ListBox
                    {
                        Height = 210,
                        ItemsSource = new[]
                        {
                            "Signal anomaly detected in sector Delta-9",
                            "Autonomous drone mesh rerouted after weather spike",
                            "Cold-start recovery completed by generated fallback lane",
                            "New domain capability badge applied to mission planner"
                        }
                    }
                }
            }
        };
        Grid.SetColumn(incidentFeed, 1);
        missionBoard.Children.Add(incidentFeed);

        Grid.SetRow(missionBoard, 1);
        workspace.Children.Add(missionBoard);

        var actionLogPanel = new Border
        {
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock { Text = "Mission command log", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Brushes.White },
                    _transformLog
                }
            }
        };
        Grid.SetRow(actionLogPanel, 2);
        workspace.Children.Add(actionLogPanel);

        Grid.SetRow(workspace, 1);
        Grid.SetColumn(workspace, 1);
        appGrid.Children.Add(workspace);

        Content = new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse("#0c1124"), 0),
                    new GradientStop(Color.Parse("#111b38"), 0.5),
                    new GradientStop(Color.Parse("#1b0d2a"), 1)
                }
            },
            Child = appGrid
        };
    }

    private Button CreateTransformNavButton(string label, string command)
    {
        var button = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(10, 8),
            Background = new SolidColorBrush(Color.Parse("#1d2c52")),
            Foreground = Brushes.White
        };
        button.Click += (_, _) => HandleCommand(command);
        return button;
    }

    private static Border CreateMetricCard(string title, string value, IBrush accent)
    {
        return new Border
        {
            Width = 190,
            BorderBrush = accent,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock { Text = title, Foreground = Brushes.LightGray },
                    new TextBlock { Text = value, FontSize = 22, FontWeight = FontWeight.Bold, Foreground = accent }
                }
            }
        };
    }

    private void RestoreNexoShell()
    {
        Title = "Nexo Avalonia Dynamic Extension Demo";
        Content = _baselineShell;
        _status.Text = "Restored baseline shell.";
        _transformPhase.Text = "Phase: Mission shell idle.";
        AppendChat("assistant", "Baseline Nexo shell restored.");
    }

    private void HandleCommand(string command)
    {
        if (string.Equals(command, "shell:restore", StringComparison.OrdinalIgnoreCase))
        {
            RestoreNexoShell();
            return;
        }

        if (command.StartsWith("nav:", StringComparison.OrdinalIgnoreCase))
            _transformPhase.Text = $"Phase: Viewing {command["nav:".Length..]} station.";
        else if (command.Contains(":wow", StringComparison.OrdinalIgnoreCase))
            _transformPhase.Text = "Phase: WOW sequence executed across every station.";
        else if (command.Contains(":deploy", StringComparison.OrdinalIgnoreCase))
            _transformPhase.Text = "Phase: Autonomous patch deployment initiated.";
        else if (command.Contains(":launch", StringComparison.OrdinalIgnoreCase))
            _transformPhase.Text = "Phase: Simulation launch accepted.";
        else if (command.Contains(":boost", StringComparison.OrdinalIgnoreCase))
            _transformPhase.Text = "Phase: Autonomy boost pipeline stabilized.";

        var line = $"{DateTimeOffset.Now:HH:mm:ss} :: {command}";
        _transformLog.Text = string.IsNullOrWhiteSpace(_transformLog.Text)
            ? line
            : $"{_transformLog.Text}{Environment.NewLine}{line}";
        AppendChat("assistant", $"Action command: {command}");
    }

    private static string ExplainNexo(string question)
    {
        var q = (question ?? string.Empty).ToLowerInvariant();
        if (q.Contains("what is nexo", StringComparison.Ordinal))
            return "Nexo is an orchestration system that scaffolds features and validates them with tests.";
        if (q.Contains("framework", StringComparison.Ordinal))
            return "UI abstractions keep feature descriptors framework-neutral so adapters can target Avalonia or MAUI.";
        return "Ask about Nexo, or scaffold a feature to see live extension loading.";
    }
}

public sealed class AvaloniaUiFrameworkAdapter : IUiFrameworkAdapter<Control>
{
    public Control Create(UiNode node, Action<string>? commandHandler = null)
    {
        return node.Kind.ToLowerInvariant() switch
        {
            "panel" => CreatePanel(node, commandHandler),
            "text" => new TextBlock { Text = node.Text },
            "button" => CreateButton(node, commandHandler),
            "badge" => CreateBadge(node),
            "progress" => CreateProgress(node),
            _ => new TextBlock { Text = $"Unsupported node kind: {node.Kind}" }
        };
    }

    private Control CreatePanel(UiNode node, Action<string>? commandHandler)
    {
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new TextBlock
        {
            Text = node.Text,
            FontWeight = FontWeight.SemiBold
        });
        if (node.Children != null)
        {
            var childContainer = new StackPanel
            {
                Spacing = 6,
                Orientation = string.Equals(node.Layout, "horizontal", StringComparison.OrdinalIgnoreCase)
                    ? Orientation.Horizontal
                    : Orientation.Vertical
            };
            foreach (var child in node.Children)
                childContainer.Children.Add(Create(child, commandHandler));
            panel.Children.Add(childContainer);
        }
        return new Border
        {
            BorderBrush = Brushes.SlateGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8),
            Child = panel
        };
    }

    private Control CreateButton(UiNode node, Action<string>? commandHandler)
    {
        var button = new Button { Content = node.Text };
        if (!string.IsNullOrWhiteSpace(node.Command))
            button.Click += (_, _) => commandHandler?.Invoke(node.Command);
        return button;
    }

    private Control CreateBadge(UiNode node)
    {
        return new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderBrush = Brushes.SlateBlue,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 2),
            Child = new TextBlock { Text = node.Text, FontSize = 12 }
        };
    }

    private Control CreateProgress(UiNode node)
    {
        var wrap = new StackPanel { Spacing = 2, Width = 200 };
        wrap.Children.Add(new TextBlock { Text = $"{node.Text}: {node.Value ?? 0:0}%" });
        wrap.Children.Add(new ProgressBar { Minimum = 0, Maximum = 100, Value = node.Value ?? 0, Height = 12 });
        return wrap;
    }
}

public sealed class AvaloniaFeatureScaffolder : IFeatureScaffolder
{
    private readonly string _repoRoot;
    private readonly string _generatedRoot;

    public AvaloniaFeatureScaffolder(string repoRoot, string generatedRoot)
    {
        _repoRoot = repoRoot;
        _generatedRoot = generatedRoot;
    }

    public async Task<FeatureDescriptor> ScaffoldAsync(string featureRequest, CancellationToken cancellationToken = default)
    {
        var objectiveToken = LooksLikeTransformRequest(featureRequest)
            ? "AVALONIA_APP_TRANSFORM"
            : "AVALONIA_FEATURE_HOTLOAD";
        var goal =
            $"{objectiveToken} Feature request: {featureRequest}. Write output descriptor under docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/GeneratedExtensions.";
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add("src/Nexo.CLI");
        psi.ArgumentList.Add("--");
        psi.ArgumentList.Add("self-extend");
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--goal");
        psi.ArgumentList.Add(goal);
        psi.ArgumentList.Add("--repo-root");
        psi.ArgumentList.Add(_repoRoot);
        psi.ArgumentList.Add("--provider");
        psi.ArgumentList.Add("mock-json");
        psi.ArgumentList.Add("--allow-mock");
        psi.ArgumentList.Add("--json");

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("Failed to start dotnet process.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"self-extend failed: {stderr}\n{stdout}");

        var featureId = Slugify(featureRequest);
        var descriptorPath = Path.Combine(_generatedRoot, $"{featureId}.json");
        if (!File.Exists(descriptorPath))
            throw new InvalidOperationException($"Generated descriptor not found: {descriptorPath}");

        var descriptorText = await File.ReadAllTextAsync(descriptorPath, cancellationToken).ConfigureAwait(false);
        var descriptor = JsonSerializer.Deserialize<FeatureDescriptor>(descriptorText, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Descriptor deserialize failed.");
        return descriptor with { SourcePath = descriptorPath };
    }

    private static bool LooksLikeTransformRequest(string request)
    {
        if (string.IsNullOrWhiteSpace(request))
            return false;

        var lower = request.ToLowerInvariant();
        return lower.Contains("transform", StringComparison.Ordinal) ||
               lower.Contains("completely", StringComparison.Ordinal) ||
               lower.Contains("another application", StringComparison.Ordinal) ||
               lower.Contains("morph", StringComparison.Ordinal) ||
               lower.Contains("replace app", StringComparison.Ordinal);
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "feature_generated";
        var lower = value.ToLowerInvariant();
        lower = System.Text.RegularExpressions.Regex.Replace(lower, @"[^a-z0-9]+", "_");
        lower = lower.Trim('_');
        if (string.IsNullOrWhiteSpace(lower))
            lower = "feature_generated";
        if (char.IsDigit(lower[0]))
            lower = $"f_{lower}";
        return lower;
    }
}
""";

    private static string BuildAvaloniaHostReadmeSource() => """
# Nexo Avalonia Dynamic Extension Demo

Linux-compatible desktop demo using Avalonia.

## Run UI
`dotnet run --project docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/Nexo.Ui.AvaloniaHost.csproj`

## Run smoke check (no GUI)
`dotnet run --project docs/UiDomainDemoGenerated/avalonia/Nexo.Ui.AvaloniaHost/Nexo.Ui.AvaloniaHost.csproj -- --smoke`

Feature requests are scaffolded through:
`nexo self-extend run --goal "AVALONIA_FEATURE_HOTLOAD ..."`
or full application transformation:
`nexo self-extend run --goal "AVALONIA_APP_TRANSFORM ..."`
""";

    private static string BuildAvaloniaFeatureDescriptorJson(string featureRequest, string featureId, string[] retainedCapabilities)
    {
        var descriptor = new
        {
            FeatureId = featureId,
            Title = featureRequest,
            RetainedDomainKnowledge = retainedCapabilities,
            WowMessage = "Live neon pulse command rail loaded from scaffolded descriptor.",
            SourcePath = $"GeneratedExtensions/{featureId}.json",
            ExperienceMode = "augment",
            Root = new
            {
                Kind = "panel",
                Id = $"panel-{featureId}",
                Text = featureRequest,
                Children = new object[]
                {
                    new { Kind = "text", Id = $"text-{featureId}", Text = "Dynamic extension loaded inside Avalonia framework." },
                    new { Kind = "progress", Id = $"progress-{featureId}", Text = "Readiness", Value = 88.0 },
                    new { Kind = "button", Id = $"button-{featureId}", Text = "Trigger wow action", Command = $"feature:{featureId}:wow" },
                    new { Kind = "badge", Id = $"badge-{featureId}", Text = string.Join(" | ", retainedCapabilities) }
                }
            }
        };

        return JsonSerializer.Serialize(descriptor, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string BuildAvaloniaAppTransformDescriptorJson(string featureRequest, string featureId, string[] retainedCapabilities)
    {
        var descriptor = new
        {
            FeatureId = featureId,
            Title = $"{featureRequest} Command Suite",
            RetainedDomainKnowledge = retainedCapabilities,
            WowMessage = "Entire shell morphed into a scaffolded mission operations app.",
            SourcePath = $"GeneratedExtensions/{featureId}.json",
            ExperienceMode = "transform",
            Root = new
            {
                Kind = "panel",
                Id = $"transform-root-{featureId}",
                Text = "Mission operations shell",
                Children = new object[]
                {
                    new { Kind = "text", Id = $"hero-{featureId}", Text = "Adaptive mission control generated live by Nexo scaffolding." },
                    new
                    {
                        Kind = "panel",
                        Id = $"readiness-grid-{featureId}",
                        Text = "Operational readiness",
                        Layout = "horizontal",
                        Children = new object[]
                        {
                            new { Kind = "progress", Id = $"radar-{featureId}", Text = "Radar", Value = 94.0 },
                            new { Kind = "progress", Id = $"drone-{featureId}", Text = "Drone mesh", Value = 87.0 },
                            new { Kind = "progress", Id = $"safety-{featureId}", Text = "Safety rails", Value = 98.0 }
                        }
                    },
                    new
                    {
                        Kind = "panel",
                        Id = $"command-rail-{featureId}",
                        Text = "Command rail",
                        Layout = "horizontal",
                        Children = new object[]
                        {
                            new { Kind = "button", Id = $"cmd-launch-{featureId}", Text = "Launch simulation", Command = $"feature:{featureId}:launch" },
                            new { Kind = "button", Id = $"cmd-boost-{featureId}", Text = "Boost autonomy", Command = $"feature:{featureId}:boost" },
                            new { Kind = "button", Id = $"cmd-wow-{featureId}", Text = "Trigger wow sequence", Command = $"feature:{featureId}:wow" },
                            new { Kind = "button", Id = $"cmd-restore-{featureId}", Text = "Return to Nexo shell", Command = "shell:restore" }
                        }
                    },
                    new
                    {
                        Kind = "panel",
                        Id = $"knowledge-{featureId}",
                        Text = "Retained domain intelligence",
                        Layout = "horizontal",
                        Children = retainedCapabilities.Select(cap => new { Kind = "badge", Id = $"cap-{featureId}-{SlugifyIdentifier(cap)}", Text = cap }).ToArray()
                    },
                    new
                    {
                        Kind = "panel",
                        Id = $"live-feed-{featureId}",
                        Text = "Live event feed",
                        Children = new object[]
                        {
                            new { Kind = "text", Id = $"event1-{featureId}", Text = "Signal lock achieved in generated subsystem lane." },
                            new { Kind = "text", Id = $"event2-{featureId}", Text = "Autonomous planner rebound from synthetic failure injection." },
                            new { Kind = "text", Id = $"event3-{featureId}", Text = "New UI capability surfaced from descriptor contract." }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(descriptor, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string BuildUiFeatureModuleSource(string featureRequest, string featureId, string[] retainedCapabilities)
    {
        var escapedRequest = JsonSerializer.Serialize(featureRequest);
        var escapedId = JsonSerializer.Serialize(featureId);
        var capabilityArrayLiteral = $"[{string.Join(", ", retainedCapabilities.Select(c => JsonSerializer.Serialize(c)))}]";
        return $$"""
export function mountFeature(host, context) {
  const card = document.createElement("article");
  card.className = "feature-card";

  const title = document.createElement("h3");
  title.textContent = context?.featureRequest ?? {{escapedRequest}};
  card.appendChild(title);

  const body = document.createElement("p");
  body.textContent = "Generated by Nexo self-scaffold pipeline and hot-loaded into the active UI shell.";
  card.appendChild(body);

  const chips = document.createElement("div");
  chips.className = "feature-chip-row";
  const retained = (context?.retainedDomainKnowledge ?? {{capabilityArrayLiteral}});
  for (const cap of retained) {
    const chip = document.createElement("span");
    chip.className = "feature-chip";
    chip.textContent = cap;
    chips.appendChild(chip);
  }
  card.appendChild(chips);

  card.dataset.featureId = context?.featureId ?? {{escapedId}};
  host.prepend(card);
}
""";
    }

    private static string BuildUiDomainKnowledgeRetentionTestSource()
    {
        const string resourceName = "Nexo.Infrastructure.Execution.Templates.UiDomainKnowledgeRetentionTests.template.cs";
        using var stream = typeof(ProviderFactory).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string BuildErrorStateSource() => """
namespace Nexo.Unity.Generated;

public sealed record GeneratedSystemErrorState(
    bool HasCompileError,
    string Message,
    string? LastKnownGoodSystemId)
{
    public static GeneratedSystemErrorState None { get; } = new(false, string.Empty, null);
}
""";

    private static string BuildInspectorSnapshotSource() => """
namespace Nexo.Unity.Generated;

public sealed record GeneratedSystemInspectorSnapshot(
    string SystemId,
    string RawGeneratedCode,
    GeneratedSystemErrorState ErrorState,
    System.DateTimeOffset GeneratedAtUtc);
""";

    private static string BuildComposableCommandContractSource() => """
namespace Nexo.CLI.Commands.SelfExtendGenerated;

public interface IComposableExtensionCommand
{
    string ExtensionId { get; }
    IReadOnlyList<string> Dependencies { get; }
}
""";

    private static string BuildExtensionCommandSource(string className, string commandName, string extensionId, string[] dependencies)
    {
        var dependencyArrayExpr = dependencies.Length == 0
            ? "Array.Empty<string>()"
            : $"new[] {{ {string.Join(", ", dependencies.Select(d => $"\"{d}\""))} }}";
        return $$"""
using System.CommandLine;
using System.Text.Json;

namespace Nexo.CLI.Commands.SelfExtendGenerated;

public sealed class {{className}} : Command, IComposableExtensionCommand
{
    public {{className}}() : base("{{commandName}}", "Self-extend generated extension command")
    {
        this.SetHandler(() =>
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                command = Name,
                extensionId = ExtensionId,
                dependencies = Dependencies
            }));
            Environment.ExitCode = 0;
        });
    }

    public string ExtensionId => "{{extensionId}}";
    public IReadOnlyList<string> Dependencies { get; } = {{dependencyArrayExpr}};
}
""";
    }

    private static string BuildBundleCommandSource((string ClassName, string CommandName)[] extensionCommands)
        => BuildBundleCommandSource("SelfExtendBundleCommand", "self-extend-bundle", extensionCommands);

    private static string BuildBundleCommandSource(
        string bundleClassName,
        string bundleCommandName,
        (string ClassName, string CommandName)[] extensionCommands)
    {
        var addLines = string.Join("\n", extensionCommands.Select(c => $"        AddCommand(new {c.ClassName}());"));
        return $$"""
using System.CommandLine;

namespace Nexo.CLI.Commands.SelfExtendGenerated;

public sealed class {{bundleClassName}} : Command
{
    public {{bundleClassName}}() : base("{{bundleCommandName}}", "Composed bundle of generated extension commands")
    {
{{addLines}}
    }
}
""";
    }

    private static string BuildExtensionCommandStructureTestSource(
        string testClassName,
        string commandClassName,
        string expectedCommandName,
        string expectedExtensionId,
        string[] expectedDependencies)
    {
        var dependencyCount = expectedDependencies.Length;
        var dependencyAssertions = expectedDependencies.Length == 0
            ? "            AssertEqual(0, composable.Dependencies.Count, \"Dependencies should be empty for this extension\");"
            : string.Join("\n", expectedDependencies.Select(d =>
                $"            AssertTrue(composable.Dependencies.Contains(\"{d}\", StringComparer.Ordinal), \"Expected dependency '{d}'\");"));

        return $$"""
using Nexo.CLI.Commands.SelfExtendGenerated;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands.SelfExtendGenerated;

public sealed class {{testClassName}} : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new {{commandClassName}}();
            AssertEqual("{{expectedCommandName}}", command.Name, "Command name should match scaffold");

            AssertTrue(command is IComposableExtensionCommand, "Command must implement IComposableExtensionCommand");
            var composable = (IComposableExtensionCommand)command;
            AssertEqual("{{expectedExtensionId}}", composable.ExtensionId, "ExtensionId should match scaffold");
            AssertEqual({{dependencyCount}}, composable.Dependencies.Count, "Dependency count should match scaffold");
{{dependencyAssertions}}

            return Task.FromResult(new TestResult
            {
                Name = nameof({{testClassName}}),
                Category = "SelfExtendGenerated",
                Passed = true,
                Message = "Generated command structure is valid."
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof({{testClassName}}),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof({{testClassName}}),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }
}
""";
    }

    private static string BuildBundleCommandStructureTestSource(string testClassName, string[] expectedCommands)
        => BuildBundleCommandStructureTestSource(
            testClassName,
            bundleClassName: "SelfExtendBundleCommand",
            expectedBundleCommandName: "self-extend-bundle",
            expectedCommands: expectedCommands);

    private static string BuildBundleCommandStructureTestSource(
        string testClassName,
        string bundleClassName,
        string expectedBundleCommandName,
        string[] expectedCommands)
    {
        var assertions = string.Join("\n", expectedCommands.Select(c =>
            $"            AssertTrue(command.Subcommands.Any(s => string.Equals(s.Name, \"{c}\", StringComparison.Ordinal)), \"Expected subcommand '{c}'\");"));
        return $$"""
using Nexo.CLI.Commands.SelfExtendGenerated;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands.SelfExtendGenerated;

public sealed class {{testClassName}} : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new {{bundleClassName}}();
            AssertEqual("{{expectedBundleCommandName}}", command.Name, "Bundle command name should match scaffold");
{{assertions}}

            return Task.FromResult(new TestResult
            {
                Name = nameof({{testClassName}}),
                Category = "SelfExtendGenerated",
                Passed = true,
                Message = "Bundle command composition is valid."
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof({{testClassName}}),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof({{testClassName}}),
                Category = "SelfExtendGenerated",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            });
        }
    }
}
""";
    }

    private static string ResolveRepoRootFromSystemPrompt(string systemPrompt)
    {
        var match = Regex.Match(systemPrompt, "\"RepoRoot\"\\s*:\\s*\"(?<root>[^\"]+)\"", RegexOptions.IgnoreCase);
        return match.Success && !string.IsNullOrWhiteSpace(match.Groups["root"].Value)
            ? match.Groups["root"].Value
            : ".";
    }

    private static string LoadCanonicalGeneratedSource(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return BuildUiDomainKnowledgeRetentionTestSource();

        try
        {
            var baseRoot = string.IsNullOrWhiteSpace(root) ? "." : root;
            var fullRoot = Path.IsPathRooted(baseRoot) ? baseRoot : Path.GetFullPath(baseRoot);
            var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath));

            if (File.Exists(fullPath))
                return File.ReadAllText(fullPath);
        }
        catch
        {
            // Fall back to embedded template below.
        }

        return BuildUiDomainKnowledgeRetentionTestSource();
    }

    private static string InferScreenType(string prompt)
    {
        if (prompt.Contains("URL:", StringComparison.OrdinalIgnoreCase)) return "Web";
        if (prompt.Contains("Terminal", StringComparison.OrdinalIgnoreCase)) return "CLI";
        return "Unknown";
    }

    private static int InferProgressPercent(string prompt)
    {
        // If prompt mentions "Goal achieved" we can push progress up a bit; else keep low.
        return prompt.Contains("Goal", StringComparison.OrdinalIgnoreCase) ? 30 : 10;
    }

    private static string InferNextActionId(string prompt)
    {
        // Parse "- <id>:" lines under Available Actions
        var match = Regex.Match(prompt, @"^\-\s*(?<id>[^:\r\n]+)\s*:", RegexOptions.Multiline);
        return match.Success ? match.Groups["id"].Value.Trim() : "wait";
    }

}

