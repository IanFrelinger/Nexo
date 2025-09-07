namespace Nexo.Feature.AI.Models;

/// <summary>
/// Extended chunk of a streaming AI model response
/// </summary>
public record ModelResponseChunkExtended : ModelResponseChunk
{
    /// <summary>
    /// Reason for finishing the stream
    /// </summary>
    public string FinishReason { get; init; } = string.Empty;
}
