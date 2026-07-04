using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Domain.Clusters;

/// <summary>
/// A reference to a brick within a cluster, with local configuration.
/// </summary>
public class ClusterBrick
{
    /// <summary>
    /// Reference to the brick definition.
    /// </summary>
    public string BrickId { get; init; } = default!;
    
    /// <summary>
    /// Local ID within this cluster (for connections).
    /// </summary>
    public string LocalId { get; init; } = default!;
    
    /// <summary>
    /// Display position within cluster visualization.
    /// </summary>
    public Position Position { get; init; } = new();
    
    /// <summary>
    /// Default implementation for this brick within this cluster.
    /// </summary>
    public ImplementationType DefaultImplementation { get; init; } = ImplementationType.Auto;
    
    /// <summary>
    /// Can instances override this brick's implementation?
    /// </summary>
    public bool AllowImplementationOverride { get; init; } = true;
    
    /// <summary>
    /// Can instances override this brick's parameters?
    /// </summary>
    public bool AllowParameterOverride { get; init; } = true;
    
    /// <summary>
    /// Static parameter values for this brick in this cluster.
    /// </summary>
    public IReadOnlyDictionary<string, object> StaticParameters { get; init; } = 
        new Dictionary<string, object>();
    
    /// <summary>
    /// Map cluster parameters to brick inputs.
    /// Key: brick input name, Value: cluster parameter name or expression.
    /// </summary>
    public IReadOnlyDictionary<string, string> ParameterMappings { get; init; } = 
        new Dictionary<string, string>();
}
