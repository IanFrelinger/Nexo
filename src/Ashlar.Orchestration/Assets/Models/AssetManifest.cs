using System.Text.Json;

namespace Ashlar.Orchestration.Assets.Models;

/// <summary>
/// Manifest of all assets in a project.
/// </summary>
public sealed record AssetManifest
{
    /// <summary>Project owning the listed assets.</summary>
    public required string ProjectId { get; init; }

    /// <summary>Generated assets included in the manifest.</summary>
    public IReadOnlyList<AssetOutput> Assets { get; init; } = Array.Empty<AssetOutput>();

    /// <summary>UTC timestamp when the manifest was generated.</summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Asset counts grouped by type.</summary>
    public IReadOnlyDictionary<AssetType, int> AssetCounts =>
        Assets.GroupBy(a => a.AssetType).ToDictionary(g => g.Key, g => g.Count());
}
