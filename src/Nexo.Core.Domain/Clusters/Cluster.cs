namespace Nexo.Core.Domain.Clusters;

/// <summary>
/// A Cluster is a prefab group of bricks that work together to solve a common problem.
/// Think of it as a "combo" in deck-building - tested, reusable, droppable onto any canvas.
/// </summary>
public class Cluster
{
    /// <summary>Stable cluster identifier.</summary>
    public string Id { get; init; } = default!;

    /// <summary>Human-readable cluster name.</summary>
    public string Name { get; init; } = default!;

    /// <summary>Semantic version of the cluster definition.</summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>Emoji or icon token for catalog UIs.</summary>
    public string Icon { get; init; } = "🔗";

    /// <summary>Short description of the cluster purpose.</summary>
    public string Description { get; init; } = default!;
    
    /// <summary>
    /// The bricks that make up this cluster.
    /// </summary>
    public IReadOnlyList<ClusterBrick> Bricks { get; init; } = [];
    
    /// <summary>
    /// Internal connections between bricks within this cluster.
    /// </summary>
    public IReadOnlyList<ClusterConnection> Connections { get; init; } = [];
    
    /// <summary>
    /// What this cluster exposes to the outside world.
    /// </summary>
    public ClusterInterface Interface { get; init; } = new();
    
    /// <summary>
    /// Configuration parameters that can vary per instance.
    /// </summary>
    public IReadOnlyList<ClusterParameter> Parameters { get; init; } = [];
    
    /// <summary>
    /// How this cluster scales (fixed count, dynamic, event-driven).
    /// </summary>
    public ScalingConfig Scaling { get; init; } = new();
    
    /// <summary>
    /// Tags for discovery and filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
    
    /// <summary>
    /// Who created this cluster and sharing settings.
    /// </summary>
    public ClusterMetadata Metadata { get; init; } = new();
}

