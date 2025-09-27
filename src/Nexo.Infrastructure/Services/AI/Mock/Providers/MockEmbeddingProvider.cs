using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.AI.Mock.Providers
{
    public partial class MockEmbeddingProvider
    {
        private readonly ILogger<MockEmbeddingProvider> _logger;

        public MockEmbeddingProvider(ILogger<MockEmbeddingProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<EmbeddingResult> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating mock embeddings for {InputCount} inputs", request.Inputs.Count);

                // Simulate processing time
                await Task.Delay(200, cancellationToken);

                var embeddings = new List<Embedding>();
                
                foreach (var input in request.Inputs)
                {
                    var embedding = GenerateMockEmbedding(input, request.Dimensions);
                    embeddings.Add(embedding);
                }

                return new EmbeddingResult
                {
                    Embeddings = embeddings,
                    TokensUsed = request.Inputs.Sum(i => i.Split(' ').Length),
                    ModelUsed = request.Model ?? "mock-embedding-model",
                    Success = true,
                    Message = "Embeddings generated successfully"
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Embedding generation was cancelled");
                return new EmbeddingResult
                {
                    Success = false,
                    Message = "Embedding generation was cancelled",
                    Errors = { "Operation was cancelled" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating mock embeddings");
                return new EmbeddingResult
                {
                    Success = false,
                    Message = $"Embedding generation failed: {ex.Message}",
                    Errors = { ex.Message }
                };
            }
        }

        private Embedding GenerateMockEmbedding(string input, int dimensions)
        {
            var random = new Random(input.GetHashCode());
            var values = new List<float>();

            for (int i = 0; i < dimensions; i++)
            {
                values.Add((float)(random.NextDouble() * 2 - 1)); // Range: -1 to 1
            }

            // Normalize the vector
            var magnitude = Math.Sqrt(values.Sum(v => v * v));
            if (magnitude > 0)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    values[i] = (float)(values[i] / magnitude);
                }
            }

            return new Embedding
            {
                Values = values,
                Input = input,
                Index = 0
            };
        }
    }
}
