using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces.RAG;

namespace Nexo.Feature.AI.Services.RAG
{
    /// <summary>
    /// Service for generating embeddings from documentation text
    /// In a production system, this would use a proper embedding model like OpenAI's text-embedding-ada-002
    /// or a local model like sentence-transformers
    /// </summary>
    public class DocumentationEmbeddingService : IDocumentationEmbeddingService
    {
        private readonly ILogger<DocumentationEmbeddingService> _logger;
        private readonly Dictionary<string, float[]> _embeddingCache;

        public DocumentationEmbeddingService(ILogger<DocumentationEmbeddingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingCache = new Dictionary<string, float[]>();
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
                
            try
            {
                // Check cache first
                var textHash = text.GetHashCode().ToString();
                if (_embeddingCache.TryGetValue(textHash, out var cachedEmbedding))
                {
                    return cachedEmbedding;
                }

                // Generate embedding using simple TF-IDF-like approach
                // In production, this would use a proper embedding model
                var embedding = GenerateSimpleEmbedding(text);

                // Cache the result
                _embeddingCache[textHash] = embedding;

                _logger.LogDebug("Generated embedding for text of length {Length}", text.Length);
                return await Task.FromResult(embedding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for text");
                throw;
            }
        }

        public async Task<IEnumerable<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts)
        {
            try
            {
                var tasks = texts.Select(text => GenerateEmbeddingAsync(text));
                var embeddings = await Task.WhenAll(tasks);

                _logger.LogDebug("Generated {Count} embeddings", embeddings.Length);
                return embeddings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embeddings for multiple texts");
                throw;
            }
        }

        private float[] GenerateSimpleEmbedding(string text)
        {
            // This is a simplified embedding generation for demonstration
            // In production, you would use a proper embedding model

            var words = text.ToLowerInvariant()
                .Split(new char[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}' }, 
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2) // Filter out very short words
                .ToList();

            // Create a vocabulary of common C#/.NET terms
            var vocabulary = new Dictionary<string, int>
            {
                // C# keywords
                {"class", 0}, {"interface", 1}, {"namespace", 2}, {"using", 3}, {"public", 4}, {"private", 5},
                {"protected", 6}, {"internal", 7}, {"static", 8}, {"async", 9}, {"await", 10}, {"var", 11},
                {"const", 12}, {"readonly", 13}, {"virtual", 14}, {"override", 15}, {"abstract", 16},
                {"sealed", 17}, {"partial", 18}, {"extern", 19}, {"unsafe", 20},
                
                // .NET types
                {"string", 21}, {"int", 22}, {"bool", 23}, {"double", 24}, {"decimal", 25}, {"float", 26},
                {"char", 27}, {"byte", 28}, {"short", 29}, {"long", 30}, {"object", 31}, {"void", 32},
                {"array", 33}, {"list", 34}, {"dictionary", 35}, {"ienumerable", 36}, {"task", 37},
                
                // .NET concepts
                {"linq", 38}, {"lambda", 39}, {"delegate", 40}, {"event", 41}, {"property", 42}, {"method", 43},
                {"constructor", 44}, {"destructor", 45}, {"exception", 46}, {"try", 47}, {"catch", 48}, {"finally", 49},
                {"foreach", 50}, {"for", 51}, {"while", 52}, {"if", 53}, {"else", 54}, {"switch", 55},
                {"case", 57}, {"default", 58}, {"break", 59}, {"continue", 60}, {"return", 61}, {"yield", 62},
                
                // Framework concepts
                {"aspnet", 63}, {"mvc", 64}, {"webapi", 65}, {"entity", 66}, {"framework", 67}, {"core", 68},
                {"dependency", 69}, {"injection", 70}, {"configuration", 71}, {"logging", 72}, {"middleware", 73},
                {"controller", 74}, {"action", 75}, {"model", 76}, {"view", 77}, {"service", 78}, {"repository", 79},
                
                // Performance and optimization
                {"performance", 80}, {"optimization", 81}, {"memory", 82}, {"garbage", 83}, {"collection", 84},
                {"threading", 85}, {"parallel", 86}, {"concurrent", 87},
                
                // Testing
                {"test", 90}, {"unit", 91}, {"integration", 92}, {"mock", 93}, {"stub", 94}, {"assert", 95},
                {"xunit", 96}, {"nunit", 97}, {"mstest", 98}, {"mocking", 99}
            };

            // Create embedding vector (100 dimensions)
            var embedding = new float[100];
            var wordCounts = new Dictionary<string, int>();

            // Count word frequencies
            foreach (var word in words)
            {
                if (wordCounts.ContainsKey(word))
                    wordCounts[word]++;
                else
                    wordCounts[word] = 1;
            }

            // Populate embedding vector
            foreach (var (word, count) in wordCounts)
            {
                if (vocabulary.TryGetValue(word, out var index))
                {
                    // TF-IDF-like weighting
                    var tf = (float)count / words.Count;
                    var idf = Math.Log((float)vocabulary.Count / (count + 1));
                    embedding[index] = tf * (float)idf;
                }
            }

            // Normalize the vector
            var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < embedding.Length; i++)
                {
                    embedding[i] = (float)(embedding[i] / magnitude);
                }
            }

            return embedding;
        }
    }
}
