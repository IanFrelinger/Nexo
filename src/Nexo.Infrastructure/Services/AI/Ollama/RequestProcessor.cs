using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.AI.Ollama;

/// <summary>
/// Processes Ollama requests and responses.
/// </summary>
public partial class RequestProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RequestProcessor> _logger;

    public RequestProcessor(HttpClient httpClient, ILogger<RequestProcessor> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            
            // Determine the model to use
            var model = GetModelFromRequest(request);
            
            // Create the Ollama request
            var ollamaRequest = CreateOllamaRequest(request, model);
            
            // Execute the request
            var response = await ExecuteOllamaRequestAsync(ollamaRequest, cancellationToken);
            
            var executionTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            var modelResponse = new ModelResponse
            {
                Response = response.Response,
                InputTokens = EstimateTokenCount(request.Input),
                OutputTokens = EstimateTokenCount(response.Response),
                ProcessingTimeMs = executionTime,
                ProviderId = "ollama",
                ModelName = model,
                Metadata = new Dictionary<string, object>
                {
                    ["done"] = response.Done,
                    ["context"] = response.Context,
                    ["cached"] = false
                }
            };
            
            return modelResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Ollama request");
            throw;
        }
    }

    public async Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync("api/tags", cancellationToken);
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            return new ModelHealthStatus
            {
                IsHealthy = response.IsSuccessStatusCode,
                Status = response.IsSuccessStatusCode ? "Healthy" : $"HTTP {response.StatusCode}",
                LastChecked = DateTime.UtcNow,
                ResponseTimeMs = responseTime,
                ErrorRate = response.IsSuccessStatusCode ? 0.0 : 1.0,
                ProviderId = "ollama"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Ollama health status");
            return new ModelHealthStatus
            {
                IsHealthy = false,
                Status = $"Error: {ex.Message}",
                LastChecked = DateTime.UtcNow,
                ResponseTimeMs = 0,
                ErrorRate = 1.0,
                ProviderId = "ollama"
            };
        }
    }

    private static string GetModelFromRequest(ModelRequest request)
    {
        if (request.Context?.TryGetValue("model", out var modelObj) == true && modelObj is string model)
        {
            return model;
        }

        return "llama2"; // Default model
    }

    private object CreateOllamaRequest(ModelRequest request, string model)
    {
        return new
        {
            model = model,
            prompt = CreatePrompt(request),
            stream = false,
            options = new
            {
                temperature = request.Context?.TryGetValue("temperature", out var tempObj) == true ? tempObj : request.Temperature,
                top_p = request.Context?.TryGetValue("top_p", out var topPObj) == true ? topPObj : 0.9,
                num_predict = request.Context?.TryGetValue("max_tokens", out var maxTokensObj) == true ? maxTokensObj : request.MaxTokens
            }
        };
    }

    private static string CreatePrompt(ModelRequest request)
    {
        var prompt = new StringBuilder();

        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            prompt.AppendLine($"System: {request.SystemPrompt}");
            prompt.AppendLine();
        }

        prompt.Append(request.Input);
        return prompt.ToString();
    }

    private async Task<OllamaResponse> ExecuteOllamaRequestAsync(object request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/generate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<OllamaResponse>(responseContent) ?? new OllamaResponse();
    }

    private static int EstimateTokenCount(string text)
    {
        // Simple estimation: ~4 characters per token
        return Math.Max(1, text.Length / 4);
    }

    private static string ComputeRequestHash(ModelRequest request)
    {
        var input = $"{request.Input}|{request.SystemPrompt}|{request.Temperature}|{request.MaxTokens}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash)[..16]; // First 16 characters
    }

    // Response models
    private class OllamaResponse
    {
        public string Model { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public bool Done { get; set; }
        public List<long> Context { get; set; } = new();
    }
}
