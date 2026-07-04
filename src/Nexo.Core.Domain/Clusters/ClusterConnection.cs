namespace Nexo.Core.Domain.Clusters;

/// <summary>
/// A connection between two bricks within a cluster.
/// </summary>
public class ClusterConnection
{
    /// <summary>Stable connection identifier within the cluster.</summary>
    public string Id { get; init; } = default!;
    
    /// <summary>
    /// Source brick local ID within this cluster.
    /// </summary>
    public string FromBrickId { get; init; } = default!;
    
    /// <summary>
    /// Source output name.
    /// </summary>
    public string FromOutput { get; init; } = default!;
    
    /// <summary>
    /// Target brick local ID within this cluster.
    /// </summary>
    public string ToBrickId { get; init; } = default!;
    
    /// <summary>
    /// Target input name.
    /// </summary>
    public string ToInput { get; init; } = default!;
    
    /// <summary>
    /// Optional condition for this connection to activate.
    /// </summary>
    public string? Condition { get; init; }
    
    /// <summary>
    /// Connection type for visualization.
    /// </summary>
    public ConnectionType Type { get; init; } = ConnectionType.Data;
}
