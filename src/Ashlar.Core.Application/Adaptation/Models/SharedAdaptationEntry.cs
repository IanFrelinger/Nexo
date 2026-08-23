namespace Ashlar.Core.Application.Adaptation.Models;

/// <summary>
/// A promoted adaptation that can be shared with peers and adopted after validation.
/// P2.3: Shared adaptation cache.
/// </summary>
public sealed class SharedAdaptationEntry
{
    /// <summary>
    /// Unique identifier for this shared entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The adaptation record.
    /// </summary>
    public required AdaptationRecord Record { get; init; }

    /// <summary>
    /// File path to content. Key = relative path, Value = file content as bytes.
    /// </summary>
    public required IReadOnlyDictionary<string, byte[]> Files { get; init; }

    /// <summary>
    /// Peer ID of the instance that broadcast this adaptation.
    /// </summary>
    public string? SourcePeerId { get; init; }

    /// <summary>
    /// When the adaptation was broadcast.
    /// </summary>
    public DateTimeOffset BroadcastAt { get; init; }
}
