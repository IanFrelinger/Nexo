namespace Nexo.Core.Application.Persistence.Ports;

/// <summary>
/// Port for entity storage and retrieval without binding to a specific database.
///
/// Applications depend on this interface; infrastructure or adapter projects
/// provide implementations (in-memory, SQLite, PostgreSQL, etc.) so that
/// storage can be swapped without changing application or domain code.
/// </summary>
/// <typeparam name="TEntity">Entity type to store.</typeparam>
/// <typeparam name="TKey">Type of the entity's key (e.g. int, long, Guid, string).</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : class where TKey : notnull
{
    /// <summary>
    /// Gets an entity by id, or null if not found.
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities. Use with care for large sets; consider paging in callers.
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an entity. Key may be assigned by the implementation (e.g. identity column).
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity. Entity must already be tracked or identifiable by key.
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity by id.
    /// </summary>
    Task RemoveAsync(TKey id, CancellationToken cancellationToken = default);
}
