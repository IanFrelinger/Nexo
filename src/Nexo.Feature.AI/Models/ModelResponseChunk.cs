namespace Nexo.Feature.AI.Models;

/// <summary>
/// A chunk of streaming model response
/// </summary>
public record ModelResponseChunk
{
    /// <summary>
    /// The chunk content
    /// </summary>
    public string Content { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether this is the final chunk
    /// </summary>
    public bool IsFinal { get; init; }
    
    /// <summary>
    /// Chunk index in the stream
    /// </summary>
    public int Index { get; init; }
    
    /// <summary>
    /// Timestamp when the chunk was generated
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional metadata for this chunk
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
