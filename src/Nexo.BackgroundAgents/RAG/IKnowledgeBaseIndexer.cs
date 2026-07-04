using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.BackgroundAgents.RAG;

/// <summary>
/// Indexes documents from file paths into the RAG vector store with optional sensitivity level.
/// </summary>
public interface IKnowledgeBaseIndexer
{
    /// <summary>
    /// Index documents from the given paths (files and/or directories).
    /// </summary>
    /// <param name="paths">File or directory paths to index.</param>
    /// <param name="defaultSensitivityLevelName">Sensitivity level to assign to indexed documents (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of documents indexed.</returns>
    Task<int> IndexDocumentsAsync(
        IEnumerable<string> paths,
        string? defaultSensitivityLevelName,
        CancellationToken cancellationToken = default);
}
