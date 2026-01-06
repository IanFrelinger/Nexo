namespace Nexo.Core.Domain.Clusters;

/// <summary>
/// Input for executing a cluster.
/// </summary>
public class ClusterInput
{
    /// <summary>
    /// Input values for cluster ports.
    /// </summary>
    public IReadOnlyDictionary<string, object> PortValues { get; init; } = 
        new Dictionary<string, object>();
    
    /// <summary>
    /// Parameter values for cluster configuration.
    /// </summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = 
        new Dictionary<string, object>();
}

