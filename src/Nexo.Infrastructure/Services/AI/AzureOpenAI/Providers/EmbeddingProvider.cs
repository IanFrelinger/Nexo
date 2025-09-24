using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.AI.AzureOpenAI.Providers
{
    public class EmbeddingProvider
    {
        private readonly ILogger<EmbeddingProvider> _logger;

        public EmbeddingProvider(ILogger<EmbeddingProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<EmbeddingResponse> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating embeddings for {TextCount} texts", request.Texts.Count);

                // Simulate Azure OpenAI embedding generation
                await Task.Delay(200, cancellationToken);

                var response = new EmbeddingResponse
                {
                    Success = true,
                    Embeddings = GenerateEmbeddings(request.Texts),
                    Model = request.Model ?? "text-embedding-ada-002",
                    Usage = new EmbeddingUsage
                    {
                        PromptTokens = EstimateTokens(string.Join(" ", request.Texts)),
                        TotalTokens = EstimateTokens(string.Join(" ", request.Texts))
                    }
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings");
                return new EmbeddingResponse
                {
                    Success = false,
                    Error = $"Embedding generation failed: {ex.Message}"
                };
            }
        }

        private List<float[]> GenerateEmbeddings(List<string> texts)
        {
            var embeddings = new List<float[]>();
            
            foreach (var text in texts)
            {
                // Generate a mock embedding vector (1536 dimensions for text-embedding-ada-002)
                var embedding = new float[1536];
                var random = new Random(text.GetHashCode());
                
                for (int i = 0; i < 1536; i++)
                {
                    embedding[i] = (float)(random.NextDouble() * 2 - 1); // Values between -1 and 1
                }
                
                embeddings.Add(embedding);
            }
            
            return embeddings;
        }

        private int EstimateTokens(string text)
        {
            return (int)Math.Ceiling(text.Length / 4.0);
        }
    }
}
