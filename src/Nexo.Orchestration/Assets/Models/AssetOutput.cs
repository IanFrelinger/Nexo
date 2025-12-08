using System.Text.Json;

namespace Nexo.Orchestration.Assets.Models;

/// <summary>
/// Output from an asset generation agent.
/// </summary>
public sealed record AssetOutput
{
    public required string AssetId { get; init; }
    public required AssetType AssetType { get; init; }
    public required string FilePath { get; init; }
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

/// <summary>
/// Types of assets that can be generated.
/// </summary>
public enum AssetType
{
    Image,
    Audio,
    Model3D,
    Shader,
    Animation,
    Texture,
    Material,
    Prefab
}

/// <summary>
/// The prompt and parameters used for generation.
/// </summary>
public sealed record GenerationPrompt
{
    public required string TextPrompt { get; init; }
    public string? NegativePrompt { get; init; }
    public string? StyleReference { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
/// Validation of generated asset against constraints.
/// </summary>
public sealed record AssetValidation
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> PassedChecks { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> FailedChecks { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Manifest of all assets in a project.
/// </summary>
public sealed record AssetManifest
{
    public required string ProjectId { get; init; }
    public IReadOnlyList<AssetOutput> Assets { get; init; } = Array.Empty<AssetOutput>();
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyDictionary<AssetType, int> AssetCounts =>
        Assets.GroupBy(a => a.AssetType).ToDictionary(g => g.Key, g => g.Count());
}

