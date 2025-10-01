using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Adapters
{
    /// <summary>
    /// HTTP client for Ollama API interactions.
    /// </summary>
    public class OllamaHttpClient : IOllamaHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _model;
        private bool _disposed;

        public OllamaHttpClient(string baseUrl = "http://localhost:11434", string model = "llama2")
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }

        public async Task<HealthCheckResult> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var models = ParseModelsResponse(content);
                    
                    return new HealthCheckResult
                    {
                        IsHealthy = true,
                        ResponseTimeMs = (int)responseTime,
                        Message = $"Ollama is healthy with {models.Count} models available",
                        Timestamp = DateTime.UtcNow,
                        Details = new Dictionary<string, object>
                        {
                            ["models"] = models,
                            ["baseUrl"] = _baseUrl,
                            ["model"] = _model
                        }
                    };
                }
                else
                {
                    return new HealthCheckResult
                    {
                        IsHealthy = false,
                        ResponseTimeMs = (int)responseTime,
                        Message = $"Ollama health check failed with status {response.StatusCode}",
                        Timestamp = DateTime.UtcNow,
                        Details = new Dictionary<string, object>
                        {
                            ["statusCode"] = (int)response.StatusCode,
                            ["reasonPhrase"] = response.ReasonPhrase ?? "Unknown"
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                    Message = $"Ollama health check failed: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Details = new Dictionary<string, object>
                    {
                        ["exception"] = ex.Message,
                        ["exceptionType"] = ex.GetType().Name
                    }
                };
            }
        }

        public async Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var healthCheck = await HealthCheckAsync(cancellationToken);
                
                if (!healthCheck.IsHealthy)
                {
                    return new InitializationResult
                    {
                        Success = false,
                        Message = $"Ollama is not healthy: {healthCheck.Message}",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Check if the model is available
                var models = healthCheck.Details.ContainsKey("models") 
                    ? (List<ModelInfo>)healthCheck.Details["models"] 
                    : new List<ModelInfo>();

                var modelAvailable = models.Any(m => m.Name == _model);
                
                if (!modelAvailable)
                {
                    return new InitializationResult
                    {
                        Success = false,
                        Message = $"Model '{_model}' is not available. Available models: {string.Join(", ", models.Select(m => m.Name))}",
                        Timestamp = DateTime.UtcNow
                    };
                }

                return new InitializationResult
                {
                    Success = true,
                    Message = $"Ollama initialized successfully with model '{_model}'",
                    Timestamp = DateTime.UtcNow,
                    Details = new Dictionary<string, object>
                    {
                        ["model"] = _model,
                        ["baseUrl"] = _baseUrl,
                        ["availableModels"] = models
                    }
                };
            }
            catch (Exception ex)
            {
                return new InitializationResult
                {
                    Success = false,
                    Message = $"Failed to initialize Ollama: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Details = new Dictionary<string, object>
                    {
                        ["exception"] = ex.Message,
                        ["exceptionType"] = ex.GetType().Name
                    }
                };
            }
        }

        public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestBody = new
                {
                    model = _model,
                    prompt = prompt,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);

                return ollamaResponse?.Response ?? "";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to generate response from Ollama: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }

        private static List<ModelInfo> ParseModelsResponse(string content)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(content);
                var models = new List<ModelInfo>();

                if (jsonDoc.RootElement.TryGetProperty("models", out var modelsArray))
                {
                    foreach (var modelElement in modelsArray.EnumerateArray())
                    {
                        var model = new ModelInfo
                        {
                            Name = modelElement.GetProperty("name").GetString() ?? "",
                            Size = modelElement.GetProperty("size").GetInt64().ToString(),
                            ModifiedAt = modelElement.GetProperty("modified_at").GetString() ?? ""
                        };
                        models.Add(model);
                    }
                }

                return models;
            }
            catch
            {
                return new List<ModelInfo>();
            }
        }
    }
}
