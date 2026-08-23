namespace Ashlar.BackgroundAgents.Configuration;

/// <summary>
/// RAG (Retrieval Augmented Generation) configuration.
/// </summary>
public class RAGConfig
{
    /// <summary>
    /// Whether RAG is enabled for this agent.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Vector store provider ("in-memory", "sqlite", "postgres", "qdrant").
    /// </summary>
    public string? VectorStoreProvider { get; set; }

    /// <summary>
    /// Vector store path or connection string.
    /// </summary>
    public string? VectorStorePath { get; set; }

    /// <summary>
    /// Maximum number of results to retrieve.
    /// </summary>
    public int MaxRetrievalResults { get; set; } = 5;

    /// <summary>
    /// Minimum similarity score (0.0-1.0).
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.7;

    /// <summary>
    /// Paths to knowledge sources (directories or files).
    /// </summary>
    public List<string>? KnowledgeSources { get; set; }

    /// <summary>
    /// Maximum sensitivity level name of sources to index.
    /// </summary>
    public string MaxSourceSensitivity { get; set; } = "Internal";
}
