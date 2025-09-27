using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using static Nexo.Feature.AI.Enums.ModelType;

namespace Nexo.Infrastructure.Services.AI
{
/// <summary>
/// Core OpenAI model provider functionality
/// </summary>
public partial class OpenAiModelProvider
{
    // IModelProvider interface implementation
    public string ProviderId => "openai";
    public string DisplayName => "OpenAI";
    public string Description => "OpenAI GPT models for text generation and chat";
    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

    public IEnumerable<ModelType> SupportedModelTypes =>
    [
        ModelType.TextGeneration,
        ModelType.CodeGeneration,
        ModelType.TextEmbedding
    ];

    public async Task<IModel> LoadModelAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
    {
        var modelInfo = await GetModelInfoAsync(modelName, cancellationToken);
        if (modelInfo == null)
        {
            throw new InvalidOperationException($"Model {modelName} not found or not available");
        }

        return new OpenAiModel(modelName, _httpClient, _logger);
    }

    public async Task<ModelInfo> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
    {
        var models = await GetAvailableModelsAsync(cancellationToken);
        return models.FirstOrDefault(m => m.Name == modelName) ?? new ModelInfo { Name = modelName, IsAvailable = false };
    }

    public async Task<ModelValidationResult> ValidateModelAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
    {
        var errors = new List<string>();

        try
        {
            var modelInfo = await GetModelInfoAsync(modelName, cancellationToken);
            if (modelInfo == null)
            {
                errors.Add($"Model {modelName} not found");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error validating model: {ex.Message}");
        }

        return new ModelValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    public async Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync("models", cancellationToken);
            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            return new ModelHealthStatus
            {
                IsHealthy = response.IsSuccessStatusCode,
                Status = response.IsSuccessStatusCode ? "Healthy" : $"HTTP {response.StatusCode}",
                LastChecked = DateTime.UtcNow,
                ResponseTimeMs = responseTime,
                ErrorRate = response.IsSuccessStatusCode ? 0.0 : 1.0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking OpenAI health status");
            return new ModelHealthStatus
            {
                IsHealthy = false,
                Status = $"Error: {ex.Message}",
                LastChecked = DateTime.UtcNow,
                ResponseTimeMs = 0,
                ErrorRate = 1.0
            };
        }
    }

    // Legacy methods for backward compatibility
    public string Name => DisplayName;
    public string ProviderType => "OpenAI";
    public bool IsEnabled => IsAvailable;
    public bool IsPrimary => true;

    public ModelCapabilities Capabilities => new ModelCapabilities
    {
        SupportsTextGeneration = true,
        SupportsCodeGeneration = true,
        SupportsAnalysis = true,
        SupportsOptimization = false,
        SupportsStreaming = true
    };
}
}
