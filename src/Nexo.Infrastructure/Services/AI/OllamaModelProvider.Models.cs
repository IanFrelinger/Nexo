using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Core.Domain.Entities.AI;

namespace Nexo.Infrastructure.Services.AI
{
    /// <summary>
    /// Model management functionality
    /// </summary>
    public partial class OllamaModelProvider
    {
        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var response = await _httpClient.GetAsync("api/tags", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(json);
                    return modelsResponse?.Models?.Select(m => new ModelInfo
                    {
                        Name = m.Name,
                        DisplayName = m.Name,
                        ModelType = GetModelType(m.Name),
                        IsAvailable = true,
                        SizeBytes = m.Size,
                        MaxContextLength = 4096,
                        Capabilities = new ModelCapabilities
                        {
                            SupportsTextGeneration = true,
                            SupportsCodeGeneration = true,
                            SupportsAnalysis = true,
                            SupportsOptimization = false,
                            SupportsStreaming = true
                        }
                    }) ?? [];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Ollama models");
            }

            return [];
        }

        public async Task<IModel> LoadModelAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var modelInfo = await GetModelInfoAsync(modelName, cancellationToken);
            if (modelInfo == null)
            {
                throw new InvalidOperationException($"Model {modelName} not found or not available");
            }

            return new OllamaModel(modelName, _httpClient, _logger);
        }

        public async Task<ModelInfo?> GetModelInfoAsync(string modelName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var models = await GetAvailableModelsAsync(cancellationToken);
            return models.FirstOrDefault(m => m.Name == modelName);
        }

        private static ModelType GetModelType(string modelId)
        {
            if (modelId.Contains("code") || modelId.Contains("codellama"))
            {
                return ModelType.CodeGeneration;
            }
            return ModelType.TextGeneration;
        }
    }
}
