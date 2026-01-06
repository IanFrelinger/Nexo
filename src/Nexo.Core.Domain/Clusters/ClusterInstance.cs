using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Clusters;

/// <summary>
/// A configured instance of a cluster.
/// </summary>
public class ClusterInstance
{
    /// <summary>
    /// Reference to the cluster definition.
    /// </summary>
    public string ClusterId { get; init; } = default!;
    
    /// <summary>
    /// Unique ID for this instance.
    /// </summary>
    public string InstanceId { get; init; } = default!;
    
    /// <summary>
    /// Display name for this instance.
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// Parameter overrides for this instance.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = 
        new Dictionary<string, object>();
    
    /// <summary>
    /// Implementation overrides per brick (if allowed by cluster).
    /// Key: brick local ID, Value: implementation type.
    /// </summary>
    public IReadOnlyDictionary<string, ImplementationType> ImplementationOverrides { get; init; } = 
        new Dictionary<string, ImplementationType>();
    
    /// <summary>
    /// Position on canvas (if visualized).
    /// </summary>
    public Position? Position { get; init; }
}

