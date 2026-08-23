using System.Text.Json;

namespace Ashlar.Orchestration.Assets.Models;

/// <summary>
/// Output from an asset generation agent.
/// 
/// Contains:
/// - Asset identification and type
/// - File path and MIME type
/// - Generation prompt used
/// - Validation results against constraints
/// - Metadata from generation service
/// - File size and generation timestamp
/// 
/// Used by asset generation agents to return generated asset information.
/// </summary>
public sealed record AssetOutput
{
    /// <summary>Unique asset identifier.</summary>
    public required string AssetId { get; init; }

    /// <summary>Kind of generated asset.</summary>
    public required AssetType AssetType { get; init; }

    /// <summary>Path to the asset file on disk.</summary>
    public required string FilePath { get; init; }

    /// <summary>MIME type of the asset file.</summary>
    public required string MimeType { get; init; }

    /// <summary>
    /// The prompt used to generate this asset.
    /// </summary>
    public required GenerationPrompt Prompt { get; init; }

    /// <summary>
    /// Validation results against spec constraints.
    /// </summary>
    public AssetValidation? Validation { get; init; }

    /// <summary>
    /// Metadata from the generation service.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Generation timestamp.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}
