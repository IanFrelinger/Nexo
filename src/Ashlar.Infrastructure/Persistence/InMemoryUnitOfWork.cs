using System.Collections.Concurrent;
using Ashlar.Core.Application.Persistence.Ports;

namespace Ashlar.Infrastructure.Persistence;

/// <summary>
/// In-memory unit of work. Each scope gets its own store; CommitAsync is a no-op.
/// Use for tests or when no durable storage is required.
/// Implements IDisposable so that DI scope disposal (synchronous) does not throw.
/// </summary>
public sealed class InMemoryUnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ConcurrentDictionary<(Type EntityType, Type KeyType), object> _repositories = new();

    /// <inheritdoc />
    public IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class where TKey : notnull
    {
        var key = (typeof(TEntity), typeof(TKey));
        var repo = _repositories.GetOrAdd(key, _ => new InMemoryRepository<TEntity, TKey>());
        return (IRepository<TEntity, TKey>)repo;
    }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <inheritdoc />
    public void Dispose() { }
}
