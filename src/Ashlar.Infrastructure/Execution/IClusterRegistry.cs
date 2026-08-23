using Ashlar.Core.Domain.Clusters;

namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// Registry for looking up clusters by ID.
/// </summary>
public interface IClusterRegistry
{
    Cluster? Get(string id);
    IReadOnlyList<Cluster> GetAll();
    void Register(Cluster cluster);
    void Unregister(string id);
}

