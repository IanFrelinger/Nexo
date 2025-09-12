using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Interfaces.RAG;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Models.RAG;

namespace Nexo.Feature.AI.Services.RAG
{
    /// <summary>
    /// RAG service for retrieving and augmenting C#/.NET documentation
    /// </summary>
    public class DocumentationRAGService : IDocumentationRAGService
    {
        private readonly ILogger<DocumentationRAGService> _logger;
        private readonly IDocumentationVectorStore _vectorStore;
        private readonly IDocumentationEmbeddingService _embeddingService;
        private readonly IModelOrchestrator _modelOrchestrator;

        public DocumentationRAGService(
            ILogger<DocumentationRAGService> logger,
            IDocumentationVectorStore vectorStore,
            IDocumentationEmbeddingService embeddingService,
            IModelOrchestrator modelOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        public async Task<RAGResponse> QueryDocumentationAsync(RAGQuery query)
        {
            try
            {
                _logger.LogInformation("Processing RAG query: {Query}", query.Query);

                // 1. Generate embedding for the query
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query.Query);

                // 2. Search for relevant documentation chunks
                var relevantChunks = await _vectorStore.SearchSimilarAsync(
                    queryEmbedding, 
                    query.MaxResults, 
                    query.SimilarityThreshold,
                    query.Filters);

                // 3. Rank and filter results
                var rankedChunks = RankDocumentationChunks(relevantChunks, query.Query);

                // 4. Generate context from retrieved chunks
                var context = BuildContextFromChunks(rankedChunks, query.ContextType);

                // 5. Generate AI response using retrieved context
                var aiResponse = await GenerateAIResponseAsync(query, context, rankedChunks);

                return new RAGResponse
                {
                    Query = query.Query,
                    Context = context,
                    RetrievedChunks = rankedChunks,
                    AIResponse = aiResponse,
                    ConfidenceScore = CalculateConfidenceScore(rankedChunks),
                    ProcessingTimeMs = 0 // Will be set by caller
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RAG query: {Query}", query.Query);
                throw;
            }
        }

        public async Task IndexDocumentationAsync(DocumentationChunk chunk)
        {
            try
            {
                _logger.LogDebug("Indexing documentation chunk: {Title}", chunk.Title);

                // Generate embedding for the chunk
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content);

                // Store in vector database
                await _vectorStore.StoreAsync(chunk, embedding);

                _logger.LogDebug("Successfully indexed documentation chunk: {Title}", chunk.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing documentation chunk: {Title}", chunk.Title);
                throw;
            }
        }

        public async Task BulkIndexDocumentationAsync(IEnumerable<DocumentationChunk> chunks)
        {
            try
            {
                _logger.LogInformation("Bulk indexing {Count} documentation chunks", chunks.Count());

                var tasks = chunks.Select(chunk => IndexDocumentationAsync(chunk));
                await Task.WhenAll(tasks);

                _logger.LogInformation("Successfully bulk indexed {Count} documentation chunks", chunks.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk indexing of documentation chunks");
                throw;
            }
        }

        private List<DocumentationChunk> RankDocumentationChunks(
            IEnumerable<DocumentationChunk> chunks, 
            string query)
        {
            return chunks
                .Select(chunk => new
                {
                    Chunk = chunk,
                    Score = CalculateRelevanceScore(chunk, query)
                })
                .OrderByDescending(x => x.Score)
                .Select(x => x.Chunk)
                .ToList();
        }

        private double CalculateRelevanceScore(DocumentationChunk chunk, string query)
        {
            var queryLower = query.ToLowerInvariant();
            var score = 0.0;

            // Title relevance
            if (chunk.Title.ToLowerInvariant().Contains(queryLower))
                score += 2.0;

            // Content relevance
            var contentLower = chunk.Content.ToLowerInvariant();
            var queryWords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var contentWords = contentLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in queryWords)
            {
                if (contentWords.Contains(word))
                    score += 1.0;
            }

            // Tag relevance
            foreach (var tag in chunk.Tags)
            {
                if (queryLower.Contains(tag.ToLowerInvariant()))
                    score += 0.5;
            }

            // Version relevance (if query mentions specific version)
            if (chunk.Version != null && queryLower.Contains(chunk.Version))
                score += 1.5;

            return score;
        }

        private string BuildContextFromChunks(
            List<DocumentationChunk> chunks, 
            DocumentationContextType contextType)
        {
            var contextBuilder = new System.Text.StringBuilder();

            switch (contextType)
            {
                case DocumentationContextType.CodeGeneration:
                    contextBuilder.AppendLine("## Relevant C#/.NET Documentation for Code Generation");
                    break;
                case DocumentationContextType.ProblemSolving:
                    contextBuilder.AppendLine("## Relevant C#/.NET Documentation for Problem Solving");
                    break;
                case DocumentationContextType.APIReference:
                    contextBuilder.AppendLine("## Relevant C#/.NET API Documentation");
                    break;
                case DocumentationContextType.PerformanceOptimization:
                    contextBuilder.AppendLine("## Performance Considerations");
                    break;
                default:
                    contextBuilder.AppendLine("## Relevant C#/.NET Documentation");
                    break;
            }

            foreach (var chunk in chunks.Take(5)) // Limit to top 5 chunks
            {
                contextBuilder.AppendLine($"### {chunk.Title}");
                contextBuilder.AppendLine($"**Version:** {chunk.Version ?? "N/A"} | **Type:** {chunk.DocumentationType}");
                contextBuilder.AppendLine();
                contextBuilder.AppendLine(chunk.Content);
                contextBuilder.AppendLine();
            }

            return contextBuilder.ToString();
        }

        private async Task<string> GenerateAIResponseAsync(
            RAGQuery query, 
            string context, 
            List<DocumentationChunk> chunks)
        {
            var prompt = $@"
You are an expert C#/.NET developer assistant. Use the following documentation context to answer the user's question accurately and comprehensively.

CONTEXT:
{context}

USER QUESTION: {query.Query}

INSTRUCTIONS:
1. Provide a detailed, accurate answer based on the documentation context
2. Include relevant code examples when appropriate
3. Mention specific C# versions or .NET runtime versions when relevant
4. If the context doesn't contain enough information, say so clearly
5. Focus on practical, actionable advice

ANSWER:";

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 2000,
                Temperature = 0.3
            };

            var response = await _modelOrchestrator.ProcessAsync(request);
            return response.Response;
        }

        private double CalculateConfidenceScore(List<DocumentationChunk> chunks)
        {
            if (!chunks.Any())
                return 0.0;

            // Calculate average relevance score
            var avgScore = chunks.Average(chunk => chunk.RelevanceScore);
            
            // Normalize to 0-1 range
            return Math.Min(avgScore / 5.0, 1.0);
        }
    }
}
