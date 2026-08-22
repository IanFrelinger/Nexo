using Ashlar.Core.Domain.Clusters;

namespace Ashlar.Core.Application.Common.Ports;

/// <summary>
/// Abstraction for resolving cluster definitions by ID.
/// Implementations live in Infrastructure.
/// </summary>
public interface IClusterStore
{
    Task<Cluster?> GetByIdAsync(string id, CancellationToken ct = default);
}
