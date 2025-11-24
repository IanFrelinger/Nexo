namespace Nexo.Core.Application.Common.Ports;

/// <summary>
/// Port for caching strategies (Decorator pattern - OCP).
/// </summary>
public interface ICacheStrategy
{
    /// <summary>
    /// Gets a value from cache.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a value in cache.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a value from cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

