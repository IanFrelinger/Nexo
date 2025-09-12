using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models.RAG;

namespace Nexo.Feature.AI.Interfaces.RAG
{
    /// <summary>
    /// Interface for RAG-based documentation retrieval and generation
    /// </summary>
    public interface IDocumentationRAGService
    {
        /// <summary>
        /// Query documentation using RAG approach
        /// </summary>
        /// <param name="query">The RAG query</param>
        /// <returns>RAG response with retrieved context and AI-generated answer</returns>
        Task<RAGResponse> QueryDocumentationAsync(RAGQuery query);

        /// <summary>
        /// Index a single documentation chunk
        /// </summary>
        /// <param name="chunk">The documentation chunk to index</param>
        Task IndexDocumentationAsync(DocumentationChunk chunk);

        /// <summary>
        /// Bulk index multiple documentation chunks
        /// </summary>
        /// <param name="chunks">The documentation chunks to index</param>
        Task BulkIndexDocumentationAsync(IEnumerable<DocumentationChunk> chunks);
    }
}
