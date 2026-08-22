namespace Ashlar.Core.Application.Persistence.Ports;

/// <summary>
/// Port for a scoped unit of work that groups repository operations and commits atomically.
///
/// Register as scoped (e.g. one per request). Applications obtain repositories from the
/// unit of work and call CommitAsync when done. Implementations can use in-memory
/// dictionaries, SQL transactions, or document-store sessions to avoid database lock-in.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Gets a repository for the given entity and key type. All repositories from this
    /// unit of work share the same scope/transaction until CommitAsync or disposal.
    /// </summary>
    IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class where TKey : notnull;

    /// <summary>
    /// Commits pending changes. No-op for in-memory; persists and commits transaction
    /// for database-backed implementations.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);
}
