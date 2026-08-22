namespace Ashlar.BackgroundAgents.RAG;

/// <summary>
/// Result of a vector search hit.
/// </summary>
/// <param name="Id">Document/chunk identifier.</param>
/// <param name="Text">Indexed text.</param>
/// <param name="Score">Similarity score (0.0-1.0, higher is more similar).</param>
/// <param name="SensitivityLevelName">Sensitivity level name of the document, or null if unmarked.</param>
public sealed record VectorSearchResult(string Id, string Text, double Score, string? SensitivityLevelName);
