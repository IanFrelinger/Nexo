using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Data.Interfaces
{
    /// <summary>
    /// Generic repository interface for clean data access
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TId">Entity ID type</typeparam>
    public interface IRepository<T, TId> where T : class
    {
        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Entity if found, null otherwise</returns>
        Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All entities</returns>
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds entities based on a predicate
        /// </summary>
        /// <param name="predicate">Filter predicate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching entities</returns>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets entities with pagination
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated entities</returns>
        Task<IPagedResult<T>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new entity
        /// </summary>
        /// <param name="entity">Entity to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Added entity</returns>
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        /// <param name="entity">Entity to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated entity</returns>
        Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity
        /// </summary>
        /// <param name="entity">Entity to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deleted, false if not found</returns>
        Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an entity exists
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if exists, false otherwise</returns>
        Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts total entities
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total count</returns>
        Task<long> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts entities based on a predicate
        /// </summary>
        /// <param name="predicate">Filter predicate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Count of matching entities</returns>
        Task<long> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets entities with ordering
        /// </summary>
        /// <typeparam name="TKey">Ordering key type</typeparam>
        /// <param name="orderBy">Ordering expression</param>
        /// <param name="ascending">True for ascending order, false for descending</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Ordered entities</returns>
        Task<IEnumerable<T>> GetOrderedAsync<TKey>(Expression<Func<T, TKey>> orderBy, bool ascending = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets entities with ordering and pagination
        /// </summary>
        /// <typeparam name="TKey">Ordering key type</typeparam>
        /// <param name="orderBy">Ordering expression</param>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="ascending">True for ascending order, false for descending</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated and ordered entities</returns>
        Task<IPagedResult<T>> GetPagedOrderedAsync<TKey>(Expression<Func<T, TKey>> orderBy, int page, int pageSize, bool ascending = true, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Paginated result interface
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    public interface IPagedResult<T>
    {
        /// <summary>
        /// Gets the items in the current page
        /// </summary>
        IEnumerable<T> Items { get; }

        /// <summary>
        /// Gets the total number of items
        /// </summary>
        long TotalCount { get; }

        /// <summary>
        /// Gets the current page number
        /// </summary>
        int Page { get; }

        /// <summary>
        /// Gets the page size
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Gets the total number of pages
        /// </summary>
        int TotalPages { get; }

        /// <summary>
        /// Gets whether there is a previous page
        /// </summary>
        bool HasPreviousPage { get; }

        /// <summary>
        /// Gets whether there is a next page
        /// </summary>
        bool HasNextPage { get; }
    }

    /// <summary>
    /// Paginated result implementation
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    public class PagedResult<T> : IPagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public long TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    /// <summary>
    /// Repository query options
    /// </summary>
    public class RepositoryQueryOptions
    {
        /// <summary>
        /// Gets or sets whether to include related entities
        /// </summary>
        public bool IncludeRelated { get; set; }

        /// <summary>
        /// Gets or sets the related entities to include
        /// </summary>
        public string[] IncludeProperties { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets whether to track changes
        /// </summary>
        public bool TrackChanges { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout for the query
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Gets or sets additional query parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
} 