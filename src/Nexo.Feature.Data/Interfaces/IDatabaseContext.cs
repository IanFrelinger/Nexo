using Microsoft.EntityFrameworkCore;
using System.Data;
using Nexo.Feature.Data.Enums;
using Nexo.Feature.Data.Models;

namespace Nexo.Feature.Data.Interfaces
{
    /// <summary>
    /// Provides abstraction over database operations with support for multiple providers
    /// </summary>
    public interface IDatabaseContext : IDisposable
    {
        /// <summary>
        /// Gets the Entity Framework DbContext for ORM operations
        /// </summary>
        DbContext EntityFrameworkContext { get; }

        /// <summary>
        /// Gets the raw database connection for Dapper operations
        /// </summary>
        IDbConnection Connection { get; }

        /// <summary>
        /// Gets the database provider type
        /// </summary>
        DatabaseProvider Provider { get; }

        /// <summary>
        /// Gets the connection string
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Begins a new database transaction
        /// </summary>
        /// <returns>The database transaction</returns>
        IDbTransaction BeginTransaction();

        /// <summary>
        /// Begins a new database transaction with the specified isolation level
        /// </summary>
        /// <param name="isolationLevel">The isolation level for the transaction</param>
        /// <returns>The database transaction</returns>
        IDbTransaction BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// Saves changes to the database (for Entity Framework)
        /// </summary>
        /// <returns>The number of affected records</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Ensures the database is created
        /// </summary>
        Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Migrates the database to the latest version
        /// </summary>
        Task MigrateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets database statistics
        /// </summary>
        /// <returns>Database statistics</returns>
        Task<DatabaseStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the database connection
        /// </summary>
        /// <returns>True if connection is successful</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    }
} 