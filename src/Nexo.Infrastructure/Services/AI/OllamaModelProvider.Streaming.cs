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
    /// Streaming functionality
    /// </summary>
    public partial class OllamaModelProvider
    {
        public async IAsyncEnumerable<ModelResponseChunk> ExecuteStreamAsync(ModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var ollamaRequest = new OllamaRequest
                {
                    Model = request.ModelId,
                    Prompt = request.Prompt,
                    Stream = true,
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

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var chunk = JsonSerializer.Deserialize<OllamaResponse>(line);
                        if (chunk?.Response != null)
                        {
                            yield return new ModelResponseChunk
                            {
                                Content = chunk.Response,
                                ModelId = request.ModelId,
                                RequestId = request.RequestId,
                                Timestamp = DateTime.UtcNow,
                                IsComplete = chunk.Done
                            };
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse Ollama streaming response: {Line}", line);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Ollama streaming request");
                throw;
            }
        }

        public async IAsyncEnumerable<ModelResponseChunk> ExecuteStreamAsync(string modelId, string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new ModelRequest
            {
                ModelId = modelId,
                Prompt = prompt,
                RequestId = Guid.NewGuid().ToString()
            };

            await foreach (var chunk in ExecuteStreamAsync(request, cancellationToken))
            {
                yield return chunk;
            }
        }
    }
}
