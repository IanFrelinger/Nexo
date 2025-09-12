using System.Threading.Tasks;
using Nexo.Feature.AI.Models.RAG;

namespace Nexo.Feature.AI.Interfaces.RAG
{
    /// <summary>
    /// Interface for ingesting documentation into the RAG system
    /// </summary>
    public interface IDocumentationIngestionService
    {
        /// <summary>
        /// Ingest C# language documentation
        /// </summary>
        /// <param name="documentationPath">Path to the documentation files</param>
        Task IngestCSharpDocumentationAsync(string documentationPath);

        /// <summary>
        /// Ingest .NET runtime documentation for a specific version
        /// </summary>
        /// <param name="documentationPath">Path to the documentation files</param>
        /// <param name="runtimeVersion">The .NET runtime version</param>
        Task IngestDotNetRuntimeDocumentationAsync(string documentationPath, string runtimeVersion);

        /// <summary>
        /// Ingest custom documentation from a file
        /// </summary>
        /// <param name="filePath">Path to the documentation file</param>
        /// <param name="metadata">Metadata for the documentation</param>
        Task IngestCustomDocumentationAsync(string filePath, DocumentationMetadata metadata);
    }
}
