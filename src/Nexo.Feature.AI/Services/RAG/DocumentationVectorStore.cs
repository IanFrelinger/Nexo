using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces.RAG;
using Nexo.Feature.AI.Models.RAG;

namespace Nexo.Feature.AI.Services.RAG
{
    /// <summary>
    /// In-memory vector store for documentation chunks
    /// In a production system, this would use a proper vector database like Pinecone, Weaviate, or FAISS
    /// </summary>
    public class DocumentationVectorStore : IDocumentationVectorStore
    {
        private readonly ILogger<DocumentationVectorStore> _logger;
        private readonly Dictionary<string, (DocumentationChunk Chunk, float[] Embedding)> _vectorStore;
        private readonly object _lockObject = new object();

        public DocumentationVectorStore(ILogger<DocumentationVectorStore> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _vectorStore = new Dictionary<string, (DocumentationChunk, float[])>();
        }

        public async Task StoreAsync(DocumentationChunk chunk, float[] embedding)
        {
            try
            {
                lock (_lockObject)
                {
                    _vectorStore[chunk.Id] = (chunk, embedding);
                }

                _logger.LogDebug("Stored documentation chunk: {Id}", chunk.Id);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing documentation chunk: {Id}", chunk.Id);
                throw;
            }
        }

        public async Task<IEnumerable<DocumentationChunk>> SearchSimilarAsync(
            float[] queryEmbedding, 
            int maxResults, 
            double similarityThreshold,
            List<DocumentationFilter> filters)
        {
            try
            {
                var results = new List<(DocumentationChunk Chunk, double Similarity)>();

                lock (_lockObject)
                {
                    foreach (var (chunk, embedding) in _vectorStore.Values)
                    {
                        // Apply filters
                        if (!MatchesFilters(chunk, filters))
                            continue;

                        // Calculate cosine similarity
                        var similarity = CalculateCosineSimilarity(queryEmbedding, embedding);

                        if (similarity >= similarityThreshold)
                        {
                            results.Add((chunk, similarity));
                        }
                    }
                }

                // Sort by similarity and return top results
                var sortedResults = results
                    .OrderByDescending(x => x.Similarity)
                    .Take(maxResults)
                    .Select(x => 
                    {
                        x.Chunk.RelevanceScore = x.Similarity;
                        return x.Chunk;
                    })
                    .ToList();

                _logger.LogDebug("Found {Count} similar documentation chunks", sortedResults.Count);
                return await Task.FromResult(sortedResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching similar documentation chunks");
                throw;
            }
        }

        public async Task<DocumentationChunk?> GetByIdAsync(string id)
        {
            try
            {
                lock (_lockObject)
                {
                    if (_vectorStore.TryGetValue(id, out var result))
                    {
                        return result.Chunk;
                    }
                }

                return await Task.FromResult<DocumentationChunk?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documentation chunk by ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<DocumentationChunk>> GetAllAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    return _vectorStore.Values.Select(x => x.Chunk).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all documentation chunks");
                throw;
            }
        }

        public async Task DeleteAsync(string id)
        {
            try
            {
                lock (_lockObject)
                {
                    _vectorStore.Remove(id);
                }

                _logger.LogDebug("Deleted documentation chunk: {Id}", id);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting documentation chunk: {Id}", id);
                throw;
            }
        }

        public async Task<int> GetCountAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    return _vectorStore.Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documentation chunk count");
                throw;
            }
        }

        private bool MatchesFilters(DocumentationChunk chunk, List<DocumentationFilter> filters)
        {
            if (filters == null || !filters.Any())
                return true;

            foreach (var filter in filters)
            {
                var fieldValue = GetFieldValue(chunk, filter.Field);
                if (!MatchesFilter(fieldValue, filter.Value, filter.Operator))
                    return false;
            }

            return true;
        }

        private string GetFieldValue(DocumentationChunk chunk, string field)
        {
            return field.ToLowerInvariant() switch
            {
                "version" => chunk.Version,
                "runtime" => chunk.Runtime,
                "type" => chunk.DocumentationType,
                "title" => chunk.Title,
                _ => string.Empty
            };
        }

        private bool MatchesFilter(string fieldValue, string filterValue, FilterOperator op)
        {
            return op switch
            {
                FilterOperator.Equals => fieldValue.Equals(filterValue, StringComparison.OrdinalIgnoreCase),
                FilterOperator.Contains => fieldValue.Contains(filterValue, StringComparison.OrdinalIgnoreCase),
                FilterOperator.StartsWith => fieldValue.StartsWith(filterValue, StringComparison.OrdinalIgnoreCase),
                FilterOperator.EndsWith => fieldValue.EndsWith(filterValue, StringComparison.OrdinalIgnoreCase),
                FilterOperator.GreaterThan => double.TryParse(fieldValue, out var fieldNum) && 
                                            double.TryParse(filterValue, out var filterNum) && fieldNum > filterNum,
                FilterOperator.LessThan => double.TryParse(fieldValue, out var fieldNum) && 
                                          double.TryParse(filterValue, out var filterNum) && fieldNum < filterNum,
                FilterOperator.In => filterValue.Split(',').Any(v => fieldValue.Equals(v.Trim(), StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
        }

        private double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length)
                throw new ArgumentException("Vectors must have the same length");

            var dotProduct = 0.0;
            var magnitudeA = 0.0;
            var magnitudeB = 0.0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            magnitudeA = Math.Sqrt(magnitudeA);
            magnitudeB = Math.Sqrt(magnitudeB);

            if (magnitudeA == 0 || magnitudeB == 0)
                return 0;

            return dotProduct / (magnitudeA * magnitudeB);
        }
    }
}
