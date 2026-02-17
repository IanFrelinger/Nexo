using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Factory for creating and managing LLM providers.
/// </summary>
public class ProviderFactory : IProviderFactory
{
    private readonly ILogger<ProviderFactory> _logger;
    private static readonly HttpClient Http = new();
    private static HttpClient? _ollamaHttp;
    private static readonly object OllamaHttpLock = new();

    private static HttpClient OllamaHttp
    {
        get
        {
            if (_ollamaHttp != null) return _ollamaHttp;
            lock (OllamaHttpLock)
            {
                if (_ollamaHttp != null) return _ollamaHttp;
                var seconds = int.TryParse(Environment.GetEnvironmentVariable("OLLAMA_TIMEOUT_SECONDS"), out var s) && s > 0 ? s : 300;
                _ollamaHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(seconds) };
                return _ollamaHttp;
            }
        }
    }
    private readonly HashSet<string> _availableProviders = new()
    {
        // Real providers (may be wired later)
        "openai",
        "azure",
        "ollama",

        // Offline/demo providers
        "mock",
        "offline",
        "mock-json",
        "echo"
    };
    
    public ProviderFactory(ILogger<ProviderFactory> logger)
    {
        _logger = logger;
    }
    
    public bool IsProviderAvailable(string provider)
    {
        provider = (provider ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(provider)) return false;

        if (!_availableProviders.Contains(provider)) return false;

        // Offline/demo providers are always available
        if (provider is "mock" or "offline" or "mock-json" or "echo") return true;

        return provider switch
        {
            "openai" => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
            "azure" => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"))
                       && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"))
                       && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")),
            "ollama" => true, // local service may or may not be running; caller can try
            _ => false
        };
    }
    
    public async Task<string> ExecuteLLMAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        object config,
        CancellationToken cancellationToken = default)
    {
        provider = (provider ?? "mock").Trim().ToLowerInvariant();
        _logger.LogInformation("Executing LLM request with provider {Provider}", provider);
        
        // Simulate latency to keep progress reporting realistic
        await Task.Delay(30, cancellationToken);
        
        // Offline/demo-safe providers: always return parseable JSON tailored to the prompt.
        if (provider is "mock" or "offline" or "mock-json" or "echo")
        {
            return GenerateMockJsonResponse(systemPrompt, userPrompt);
        }
        
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
            return await ExecuteOllamaAsync(systemPrompt, userPrompt, cancellationToken);

        throw new InvalidOperationException($"Unknown or unsupported provider: {provider}. Use mock, offline, openai, azure, or ollama.");
    }

    private async Task<string> ExecuteOpenAiAsync(string apiKey, string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
        var url = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1/chat/completions";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt ?? "" },
                new { role = "user", content = userPrompt ?? "" }
            },
            temperature = 0.2
        };

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

        return content ?? throw new InvalidOperationException("OpenAI response content was null");
    }

    private async Task<string> ExecuteAzureOpenAiAsync(string endpoint, string apiKey, string deployment, string systemPrompt, string userPrompt, CancellationToken ct)
    {
        endpoint = endpoint.TrimEnd('/');
        var apiVersion = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_VERSION") ?? "2024-06-01";
        var url = $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("api-key", apiKey);

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt ?? "" },
                new { role = "user", content = userPrompt ?? "" }
            },
            temperature = 0.2
        };

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

        return content ?? throw new InvalidOperationException("Azure OpenAI response content was null");
    }

    private async Task<string> ExecuteOllamaAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        return await ExecuteOllamaAsync(systemPrompt, userPrompt, (IReadOnlyList<byte[]>?)null, ct);
    }

    private async Task<string> ExecuteOllamaAsync(string systemPrompt, string userPrompt, byte[]? imageBytes, CancellationToken ct)
    {
        return await ExecuteOllamaAsync(systemPrompt, userPrompt, imageBytes != null ? [imageBytes] : null, ct);
    }

    private async Task<string> ExecuteOllamaAsync(string systemPrompt, string userPrompt, IReadOnlyList<byte[]>? imageBytesList, CancellationToken ct)
    {
        var baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434";
        baseUrl = baseUrl.TrimEnd('/');
        var hasImages = imageBytesList is { Count: > 0 };
        // Vision: prefer SmolVLM2 (smol, 2.2B, video-capable); fallback llava. Override with OLLAMA_VISION_MODEL.
        var requestedModel = hasImages
            ? (Environment.GetEnvironmentVariable("OLLAMA_VISION_MODEL") ?? Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "richardyoung/smolvlm2-2.2b-instruct")
            : (Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.1");
        var model = await ResolveOllamaModelAsync(baseUrl, requestedModel, hasImages, ct);
        var url = $"{baseUrl}/api/chat";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);

        // Build message payload - support single or multiple images
        object userMessage;
        if (hasImages && imageBytesList != null)
        {
            var imageBase64Array = imageBytesList
                .Where(b => b != null && b.Length > 0)
                .Select(b => Convert.ToBase64String(b!))
                .ToArray();
            userMessage = new
            {
                role = "user",
                content = userPrompt ?? "",
                images = imageBase64Array
            };
        }
        else
        {
            userMessage = new
            {
                role = "user",
                content = userPrompt ?? ""
            };
        }

        var payload = new
        {
            model,
            stream = false,
            messages = new[]
            {
                new { role = "system", content = systemPrompt ?? "" },
                userMessage
            }
        };

        req.Content = new StringContent(JsonSerializer.Serialize(payload));
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var resp = await OllamaHttp.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var available = await GetOllamaModelNamesAsync(baseUrl, ct);
            throw new InvalidOperationException(
                $"Ollama model '{model}' not found (404). Available: {string.Join(", ", available)}. " +
                "Pull a model with: ollama pull " + (hasImages ? "richardyoung/smolvlm2-2.2b-instruct" : "llama3.2:3b") + " (or llava:7b)");
        }

        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? throw new InvalidOperationException("Ollama response content was null");
    }

    /// <summary>
    /// Resolve the requested model name to an available Ollama model. If the requested model
    /// is not found, pick the first available vision or text model so the agent can run without manual config.
    /// </summary>
    private async Task<string> ResolveOllamaModelAsync(string baseUrl, string requestedModel, bool forVision, CancellationToken ct)
    {
        var available = await GetOllamaModelNamesAsync(baseUrl, ct);
        if (available.Count == 0)
            return requestedModel;

        var exact = available.FirstOrDefault(m => string.Equals(m, requestedModel, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact;

        if (forVision)
        {
            var vision = available.FirstOrDefault(m => m.Contains("smolvlm", StringComparison.OrdinalIgnoreCase))
                ?? available.FirstOrDefault(m => m.Contains("llava", StringComparison.OrdinalIgnoreCase));
            if (vision != null)
            {
                _logger.LogInformation("Ollama model '{Requested}' not found; using vision model '{Resolved}'", requestedModel, vision);
                return vision;
            }
        }
        else
        {
            var text = available.FirstOrDefault(m => m.Contains("llama", StringComparison.OrdinalIgnoreCase))
                ?? available.FirstOrDefault();
            if (text != null)
            {
                _logger.LogInformation("Ollama model '{Requested}' not found; using '{Resolved}'", requestedModel, text);
                return text;
            }
        }

        var fallback = available[0];
        _logger.LogInformation("Ollama model '{Requested}' not found; using '{Resolved}'", requestedModel, fallback);
        return fallback;
    }

    private async Task<List<string>> GetOllamaModelNamesAsync(string baseUrl, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/tags");
            using var resp = await OllamaHttp.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            var list = new List<string>();
            if (doc.RootElement.TryGetProperty("models", out var models))
            {
                foreach (var m in models.EnumerateArray())
                {
                    var name = m.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (!string.IsNullOrEmpty(name))
                        list.Add(name);
                }
            }
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not list Ollama models at {BaseUrl}/api/tags", baseUrl);
            return new List<string>();
        }
    }

    /// <summary>
    /// Execute LLM with vision support (image analysis).
    /// </summary>
    public async Task<string> ExecuteVisionAsync(
        string provider,
        string systemPrompt,
        string userPrompt,
        byte[] imageBytes,
        object config,
        CancellationToken cancellationToken = default)
    {
        provider = (provider ?? "mock").Trim().ToLowerInvariant();
        _logger.LogInformation("Executing vision request with provider {Provider}", provider);

        if (provider is "ollama" or "auto" or "local")
            return await ExecuteOllamaAsync(systemPrompt, userPrompt, imageBytes, cancellationToken);

        if (provider is "openai" or "azure")
            throw new NotSupportedException($"Vision API not yet implemented for provider: {provider}. Use ollama with a vision model (e.g. richardyoung/smolvlm2-2.2b-instruct, llava:7b).");

        throw new InvalidOperationException($"Unknown or unsupported vision provider: {provider}. Use ollama, auto, or local.");
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
        provider = (provider ?? "mock").Trim().ToLowerInvariant();
        var frames = (frameBytes ?? Array.Empty<byte[]>()).Where(b => b != null && b.Length > 0).ToList();

        if (frames.Count == 0)
            throw new ArgumentException("At least one non-empty frame is required.", nameof(frameBytes));

        // Single frame: delegate to existing path
        if (frames.Count == 1)
            return await ExecuteVisionAsync(provider, systemPrompt, userPrompt, frames[0], config, cancellationToken);

        _logger.LogInformation("Executing multi-frame vision request with provider {Provider}, {Count} frames", provider, frames.Count);

        // Mock/echo: use last frame only (poor man's fallback)
        if (provider is "mock" or "offline" or "mock-json" or "echo")
            return await ExecuteVisionAsync(provider, systemPrompt, userPrompt + $"\n[Note: {frames.Count} frames provided, analyzing most recent.]", frames[^1], config, cancellationToken);

        if (provider is "ollama" or "auto" or "local")
            return await ExecuteOllamaAsync(systemPrompt, userPrompt, frames, cancellationToken);

        if (provider is "openai" or "azure")
            throw new NotSupportedException($"Multi-frame vision not yet implemented for provider: {provider}. Use ollama.");

        throw new InvalidOperationException($"Unknown or unsupported vision provider: {provider}.");
    }

    /// <inheritdoc />
    public async Task EnsureOllamaReachableAsync(bool requireVisionModel, CancellationToken cancellationToken = default)
    {
        var baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434";
        baseUrl = baseUrl.TrimEnd('/');
        List<string> models;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/tags");
            using var resp = await OllamaHttp.SendAsync(req, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);
            models = new List<string>();
            if (doc.RootElement.TryGetProperty("models", out var arr))
            {
                foreach (var m in arr.EnumerateArray())
                {
                    var name = m.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (!string.IsNullOrEmpty(name)) models.Add(name);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Ollama is not reachable at {baseUrl}. Start Ollama (e.g. ollama serve or: docker run -d -p 11434:11434 ollama/ollama). {ex.Message}", ex);
        }

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

        // Visual validation for 3D model rendering
        if (systemPrompt.Contains("3D graphics", StringComparison.OrdinalIgnoreCase) 
            || systemPrompt.Contains("visual validation", StringComparison.OrdinalIgnoreCase)
            || userPrompt.Contains("terrain mesh", StringComparison.OrdinalIgnoreCase)
            || userPrompt.Contains("building mesh", StringComparison.OrdinalIgnoreCase)
            || userPrompt.Contains("world bundle", StringComparison.OrdinalIgnoreCase))
        {
            // For synthetic data (echo provider), assume rendering is correct
            // Real vision models would analyze the actual screenshot
            var obj = new
            {
                passed = true,
                reasoning = "Synthetic data (echo provider) generates valid 3D meshes. Visual validation passed for mock data. For production, use vision-capable models (GPT-4o, Claude 3 Opus) to analyze actual screenshots.",
                issues = Array.Empty<object>(),
                confidence = 0.85
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

        // Autonomous Dev bricks
        if (systemPrompt.Contains("create a clear specification", StringComparison.OrdinalIgnoreCase)
            || systemPrompt.Contains("analyzing a development task to create a clear specification", StringComparison.OrdinalIgnoreCase))
        {
            var summary = InferTaskSummary(userPrompt);
            var obj = new
            {
                summary,
                changeType = "Test",
                functionalRequirements = new[]
                {
                    new { id = "req1", description = "Create a small, safe change suitable for demo runs.", priority = "Must", isMandatory = true }
                },
                nonFunctionalRequirements = Array.Empty<object>(),
                acceptanceCriteria = new[]
                {
                    new { id = "ac1", description = "Change is applied and project still builds.", testDescription = "Run build successfully", isAutomatable = true }
                },
                affectedAreas = new[] { "." },
                risks = new[] { "Offline provider returns template JSON; wire real providers for production." },
                complexity = 1,
                confidence = 0.7,
                openQuestions = Array.Empty<string>()
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("planning how to implement a feature", StringComparison.OrdinalIgnoreCase))
        {
            // Create a plan that is safe for most projects: add/update a markdown note file.
            var targetFile = InferTargetFileFromPrompt(userPrompt) ?? "NEXO_AGENT_NOTES.md";
            var obj = new
            {
                tasks = new[]
                {
                    new
                    {
                        id = "task1",
                        title = "Write a demo notes file",
                        description = "Create/update a small markdown file for demo purposes (safe, non-breaking).",
                        type = "ModifyFile",
                        steps = new[] { "Write file content", "Build project" },
                        targetFiles = new[] { targetFile },
                        verificationMethod = "dotnet build (or project build) succeeds"
                    }
                },
                dependencies = Array.Empty<object>(),
                plannedChanges = new[]
                {
                    new { filePath = targetFile, changeType = "Modify", description = "Add demo note content" }
                },
                testStrategy = new
                {
                    testCases = new[] { "Build succeeds" },
                    persona = "Average",
                    userFlows = Array.Empty<string>(),
                    minimumPassRate = 0.9
                },
                estimatedDuration = "00:01:00"
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("implementing a development task", StringComparison.OrdinalIgnoreCase))
        {
            var targetPath = InferTargetFileFromPrompt(userPrompt) ?? "NEXO_AGENT_NOTES.md";
            var content = $"# Nexo Demo Notes\n\nGenerated by offline/mock-json provider at {DateTime.UtcNow:O}.\n\nThis file is safe to modify and should not break builds.\n";
            var obj = new[]
            {
                new
                {
                    targetPath,
                    content,
                    description = "Offline demo artifact",
                    language = "markdown",
                    isFullFile = true,
                    confidence = 0.8,
                    uncertainties = Array.Empty<string>()
                }
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("analyzing test results to provide developer feedback", StringComparison.OrdinalIgnoreCase))
        {
            var score = InferScoreFromPrompt(userPrompt);
            var overallSuccess = score >= 80;
            var obj = new
            {
                overallSuccess,
                acceptanceScore = score,
                criteriaResults = Array.Empty<object>(),
                userExperience = new
                {
                    narrative = "Offline/mock feedback generated.",
                    intuitivenessScore = 7,
                    userFrustrated = false,
                    timeToComplete = "00:00:10",
                    attemptCount = 1,
                    overallReaction = "OK"
                },
                issues = Array.Empty<object>(),
                actionableItems = Array.Empty<object>()
            };
            return JsonSerializer.Serialize(obj);
        }

        if (systemPrompt.Contains("analyzing test feedback to decide what to do next", StringComparison.OrdinalIgnoreCase))
        {
            var score = InferScoreFromPrompt(userPrompt);
            var decision = score >= 90 ? "Complete" : "Iterate";
            var obj = new
            {
                decision,
                reasoning = score >= 90 ? "Acceptance score is high enough." : "Acceptance score below threshold; iterate once.",
                plannedFixes = Array.Empty<object>(),
                confidence = 0.7,
                estimatedRemainingIterations = score >= 90 ? 0 : 1
            };
            return JsonSerializer.Serialize(obj);
        }

        // Fallback: return a benign JSON object
        return "{}";
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

    private static string InferTaskSummary(string prompt)
    {
        // Try to find "## Task Description" section
        var match = Regex.Match(prompt, @"## Task Description\s*(?<task>[\s\S]*?)(\n## |\z)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var task = match.Groups["task"].Value.Trim();
            return task.Length > 120 ? task[..120] + "…" : task;
        }
        return "Demo task (offline)";
    }

    private static string? InferTargetFileFromPrompt(string prompt)
    {
        // For generation prompt: lines under "## Target Files"
        var match = Regex.Match(prompt, @"## Target Files\s*(?<block>[\s\S]*?)(\n## |\z)", RegexOptions.IgnoreCase);
        if (!match.Success) return null;

        var block = match.Groups["block"].Value;
        var line = block.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith("- "));
        if (line == null) return null;
        return line[2..].Trim();
    }

    private static double InferScoreFromPrompt(string prompt)
    {
        // Try to parse "Score: <number>%"
        var match = Regex.Match(prompt, @"Score:\s*(?<score>\d+)", RegexOptions.IgnoreCase);
        if (match.Success && double.TryParse(match.Groups["score"].Value, out var score)) return score;
        return 85;
    }
}

