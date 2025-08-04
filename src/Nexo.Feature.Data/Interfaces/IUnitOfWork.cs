using System.Data;
using Nexo.Feature.Data.Models;

namespace Nexo.Feature.Data.Interfaces
{
    /// <summary>
    /// Unit of Work pattern interface for managing transactions and repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Gets a repository for the specified entity type
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <typeparam name="TKey">The primary key type</typeparam>
        /// <returns>The repository instance</returns>
        IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class;

        /// <summary>
        /// Begins a new transaction
        /// </summary>
        /// <returns>The transaction</returns>
        IDbTransaction BeginTransaction();

        /// <summary>
        /// Begins a new transaction with the specified isolation level
        /// </summary>
        /// <param name="isolationLevel">The isolation level</param>
        /// <returns>The transaction</returns>
        IDbTransaction BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// Commits all changes in the current transaction
        /// </summary>
        /// <returns>Number of affected records</returns>
        Task<int> CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        void Rollback();

        /// <summary>
        /// Gets the database context
        /// </summary>
        IDatabaseContext DatabaseContext { get; }

        /// <summary>
        /// Checks if there are any unsaved changes
        /// </summary>
        /// <returns>True if there are unsaved changes</returns>
        bool HasChanges();

        /// <summary>
        /// Gets the change tracker for monitoring entity changes
        /// </summary>
        IChangeTracker ChangeTracker { get; }

        /// <summary>
        /// Detaches all entities from the context
        /// </summary>
        void DetachAll();

        /// <summary>
        /// Refreshes an entity from the database
        /// </summary>
        /// <param name="entity">The entity to refresh</param>
        Task RefreshAsync(object entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets database statistics
        /// </summary>
        /// <returns>Database statistics</returns>
        Task<DatabaseStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }
} 