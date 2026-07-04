using Nexo.Core.Domain.Clusters;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// In-memory registry for clusters.
/// </summary>
public class ClusterRegistry : IClusterRegistry
{
    private readonly Dictionary<string, Cluster> _clusters = new();
    
    /// <summary>Initializes a new cluster registry.</summary>
    public ClusterRegistry(IEnumerable<Cluster>? clusters = null)
    {
        if (clusters != null)
        {
            foreach (var cluster in clusters)
            {
                _clusters[cluster.Id] = cluster;
            }
        }
    }
    
    /// <summary>Gets .</summary>
    public Cluster? Get(string id)
    {
        return _clusters.TryGetValue(id, out var cluster) ? cluster : null;
    }
    
    /// <summary>Gets all.</summary>
    public IReadOnlyList<Cluster> GetAll()
    {
        return _clusters.Values.ToList();
    }
    
    /// <summary>Registers .</summary>
    public void Register(Cluster cluster)
    {
        _clusters[cluster.Id] = cluster;
    }
    
    /// <summary>Unregister.</summary>
    public void Unregister(string id)
    {
        _clusters.Remove(id);
    }
}

