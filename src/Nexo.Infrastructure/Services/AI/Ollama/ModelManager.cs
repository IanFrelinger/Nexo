using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Entities.AI;

namespace Nexo.Infrastructure.Services.AI.Ollama;

/// <summary>
/// Manages Ollama model operations (load, unload, download, remove).
/// </summary>
public class ModelManager
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ModelManager> _logger;
    private readonly HashSet<string> _loadedModels = new();

    public ModelManager(HttpClient httpClient, ILogger<ModelManager> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsModelLoaded(string modelName) => _loadedModels.Contains(modelName);

    public async Task LoadModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        if (IsModelLoaded(modelName))
        {
            _logger.LogDebug("Model {ModelName} is already loaded", modelName);
            return;
        }

        try
        {
            _logger.LogInformation("Loading model {ModelName} into Ollama", modelName);
            
            var request = new
            {
                name = modelName,
                stream = false
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/generate", content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _loadedModels.Add(modelName);
                _logger.LogInformation("Successfully loaded model {ModelName}", modelName);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to load model {modelName}: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading model {ModelName}", modelName);
            throw;
        }
    }

    public async Task UnloadModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        if (!IsModelLoaded(modelName))
        {
            _logger.LogDebug("Model {ModelName} is not loaded", modelName);
            return;
        }

        try
        {
            _logger.LogInformation("Unloading model {ModelName} from Ollama", modelName);
            
            // Ollama doesn't have a specific unload API, but we can remove from our tracking
            _loadedModels.Remove(modelName);
            
            _logger.LogInformation("Successfully unloaded model {ModelName}", modelName);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading model {ModelName}", modelName);
            throw;
        }
    }

    public async Task<long> GetModelMemoryUsageAsync(string modelName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/ps", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var psResponse = JsonSerializer.Deserialize<OllamaPsResponse>(json);
                
                var model = psResponse?.Models?.FirstOrDefault(m => m.Name == modelName);
                return model?.Size ?? 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting memory usage for model {ModelName}", modelName);
        }

        return 0;
    }

    public async Task<ModelInfo> DownloadModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading model {ModelName} via Ollama", modelName);
            
            var request = new
            {
                name = modelName,
                stream = false
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/pull", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Get model info after download
            var models = await GetAvailableModelsAsync(cancellationToken);
            var modelInfo = models.FirstOrDefault(m => m.Name == modelName);
            
            if (modelInfo == null)
            {
                throw new InvalidOperationException($"Model {modelName} was downloaded but not found in available models");
            }

            _logger.LogInformation("Successfully downloaded model {ModelName}", modelName);
            return modelInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading model {ModelName}", modelName);
            throw;
        }
    }

    public async Task<bool> RemoveModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing model {ModelName} via Ollama", modelName);
            
            var request = new
            {
                name = modelName
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.DeleteAsync($"api/delete", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _loadedModels.Remove(modelName);
                _logger.LogInformation("Successfully removed model {ModelName}", modelName);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing model {ModelName}", modelName);
            return false;
        }
    }

    public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("api/tags", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(json);
                
                var models = modelsResponse?.Models?.Select(m => new ModelInfo
                {
                    Name = m.Name,
                    SizeBytes = m.Size,
                    Capabilities = new Dictionary<string, object>
                    {
                        ["SupportsTextGeneration"] = true,
                        ["SupportsCodeGeneration"] = m.Name.Contains("code") || m.Name.Contains("codellama"),
                        ["SupportsAnalysis"] = true,
                        ["SupportsOptimization"] = false,
                        ["SupportsStreaming"] = true
                    }
                }) ?? [];
                
                return models;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Ollama models");
        }

        return [];
    }

    // Response models
    private class OllamaModelsResponse
    {
        public List<OllamaModelData> Models { get; set; } = new();
    }

    private class OllamaModelData
    {
        public string Name { get; set; } = string.Empty;
        public string Digest { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    private class OllamaPsResponse
    {
        public List<OllamaModelProcess> Models { get; set; } = new();
    }

    private class OllamaModelProcess
    {
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
    }
}
