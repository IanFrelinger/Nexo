using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Execution;

/// <summary>
/// Semantic cache for storing and retrieving brick execution results.
/// </summary>
public interface ISemanticCache
{
    Task<BrickOutput?> GetAsync(string cacheKey, CancellationToken cancellationToken = default);
    Task SetAsync(string cacheKey, BrickOutput output, CancellationToken cancellationToken = default);
}

