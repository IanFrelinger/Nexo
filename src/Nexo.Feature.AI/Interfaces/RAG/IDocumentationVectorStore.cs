using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models.RAG;

namespace Nexo.Feature.AI.Interfaces.RAG
{
    /// <summary>
    /// Interface for vector storage of documentation chunks
    /// </summary>
    public interface IDocumentationVectorStore
    {
        /// <summary>
        /// Store a documentation chunk with its embedding
        /// </summary>
        /// <param name="chunk">The documentation chunk</param>
        /// <param name="embedding">The vector embedding</param>
        Task StoreAsync(DocumentationChunk chunk, float[] embedding);

        /// <summary>
        /// Search for similar documentation chunks
        /// </summary>
        /// <param name="queryEmbedding">The query embedding vector</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="similarityThreshold">Minimum similarity threshold</param>
        /// <param name="filters">Optional filters to apply</param>
        /// <returns>List of similar documentation chunks</returns>
        Task<IEnumerable<DocumentationChunk>> SearchSimilarAsync(
            float[] queryEmbedding, 
            int maxResults, 
            double similarityThreshold,
            List<DocumentationFilter> filters);

        /// <summary>
        /// Get a documentation chunk by ID
        /// </summary>
        /// <param name="id">The chunk ID</param>
        /// <returns>The documentation chunk or null if not found</returns>
        Task<DocumentationChunk?> GetByIdAsync(string id);

        /// <summary>
        /// Get all documentation chunks
        /// </summary>
        /// <returns>All documentation chunks</returns>
        Task<IEnumerable<DocumentationChunk>> GetAllAsync();

        /// <summary>
        /// Delete a documentation chunk by ID
        /// </summary>
        /// <param name="id">The chunk ID</param>
        Task DeleteAsync(string id);

        /// <summary>
        /// Get the total count of stored chunks
        /// </summary>
        /// <returns>The count of stored chunks</returns>
        Task<int> GetCountAsync();
    }
}
