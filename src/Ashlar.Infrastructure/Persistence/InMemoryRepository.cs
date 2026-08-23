using System.Collections.Concurrent;
using Ashlar.Core.Application.Persistence.Models;
using Ashlar.Core.Application.Persistence.Ports;

namespace Ashlar.Infrastructure.Persistence;

/// <summary>
/// In-memory implementation of IRepository for tests and default/single-process scenarios.
/// Entities should implement IEntity{TKey} so the repository can extract keys for Add/Update.
/// </summary>
public sealed class InMemoryRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TEntity> _store = new();

    /// <inheritdoc />
    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_store.TryGetValue(id, out var e) ? e : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<TEntity>>(_store.Values.ToList());
    }

    /// <inheritdoc />
    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = GetKey(entity);
        if (!_store.TryAdd(key, entity))
            throw new InvalidOperationException($"An entity with id '{key}' already exists.");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = GetKey(entity);
        _store[key] = entity;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(TKey id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    private static TKey GetKey(TEntity entity)
    {
        if (entity is IEntity<TKey> e)
            return e.Id;
        throw new InvalidOperationException(
            $"Entity type {typeof(TEntity).Name} must implement IEntity<{typeof(TKey).Name}> for in-memory repository.");
    }
}
