using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Feature.AI.Interfaces.RAG
{
    /// <summary>
    /// Interface for generating embeddings from documentation text
    /// </summary>
    public interface IDocumentationEmbeddingService
    {
        /// <summary>
        /// Generate an embedding vector for the given text
        /// </summary>
        /// <param name="text">The text to generate embedding for</param>
        /// <returns>Embedding vector</returns>
        Task<float[]> GenerateEmbeddingAsync(string text);

        /// <summary>
        /// Generate embedding vectors for multiple texts
        /// </summary>
        /// <param name="texts">The texts to generate embeddings for</param>
        /// <returns>Collection of embedding vectors</returns>
        Task<IEnumerable<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts);
    }
}
