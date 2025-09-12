using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models.RAG;

namespace Nexo.Feature.AI.Interfaces.RAG
{
    /// <summary>
    /// Interface for parsing documentation content into chunks
    /// </summary>
    public interface IDocumentationParser
    {
        /// <summary>
        /// Parse documentation content into chunks
        /// </summary>
        /// <param name="content">The documentation content</param>
        /// <param name="metadata">Metadata for the documentation</param>
        /// <returns>Collection of documentation chunks</returns>
        Task<IEnumerable<DocumentationChunk>> ParseDocumentationAsync(string content, DocumentationMetadata metadata);
    }
}
