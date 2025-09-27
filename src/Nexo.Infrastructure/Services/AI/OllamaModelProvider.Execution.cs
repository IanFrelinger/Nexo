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
    /// Model execution functionality
    /// </summary>
    public partial class OllamaModelProvider
    {
        public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var ollamaRequest = new OllamaRequest
                {
                    Model = request.ModelId,
                    Prompt = request.Prompt,
                    Stream = false,
                    Options = new OllamaOptions
                    {
                        Temperature = request.Temperature,
                        TopP = request.TopP,
                        MaxTokens = request.MaxTokens
                    }
                };

                var json = JsonSerializer.Serialize(ollamaRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/generate", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

                return new ModelResponse
                {
                    Content = ollamaResponse?.Response ?? string.Empty,
                    ModelId = request.ModelId,
                    RequestId = request.RequestId,
                    Timestamp = DateTime.UtcNow,
                    Usage = new ModelUsage
                    {
                        PromptTokens = request.Prompt?.Length / 4 ?? 0,
                        CompletionTokens = ollamaResponse?.Response?.Length / 4 ?? 0,
                        TotalTokens = (request.Prompt?.Length / 4 ?? 0) + (ollamaResponse?.Response?.Length / 4 ?? 0)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Ollama model request");
                throw;
            }
        }

        public async Task<ModelResponse> ExecuteAsync(string modelId, string prompt, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new ModelRequest
            {
                ModelId = modelId,
                Prompt = prompt,
                RequestId = Guid.NewGuid().ToString()
            };

            return await ExecuteAsync(request, cancellationToken);
        }

        public async Task<ModelResponse> ExecuteAsync(string modelId, string prompt, float temperature = 0.7f, int maxTokens = 1000, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new ModelRequest
            {
                ModelId = modelId,
                Prompt = prompt,
                Temperature = temperature,
                MaxTokens = maxTokens,
                RequestId = Guid.NewGuid().ToString()
            };

            return await ExecuteAsync(request, cancellationToken);
        }
    }
}
