using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Domain.Clusters;
using Nexo.Infrastructure.Execution;

namespace Nexo.Infrastructure.Workflows;

/// <summary>
/// Adapter that implements IClusterStore by delegating to IClusterRegistry.
/// </summary>
public class ClusterStoreAdapter : IClusterStore
{
    private readonly IClusterRegistry _registry;

    /// <summary>Initializes a new cluster store adapter.</summary>
    public ClusterStoreAdapter(IClusterRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>Get by id asynchronously.</summary>
    public Task<Cluster?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return Task.FromResult(_registry.Get(id));
    }
}
