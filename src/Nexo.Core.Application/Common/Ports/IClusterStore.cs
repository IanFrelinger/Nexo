using Nexo.Core.Domain.Clusters;

namespace Nexo.Core.Application.Common.Ports;

/// <summary>
/// Abstraction for resolving cluster definitions by ID.
/// Implementations live in Infrastructure.
/// </summary>
public interface IClusterStore
{
    Task<Cluster?> GetByIdAsync(string id, CancellationToken ct = default);
}
