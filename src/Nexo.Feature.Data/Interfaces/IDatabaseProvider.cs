using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Data.Interfaces
{
    /// <summary>
    /// Interface for database provider abstraction supporting multiple database types
    /// </summary>
    public interface IDatabaseProvider
    {
        /// <summary>
        /// Gets the database type this provider supports
        /// </summary>
        DatabaseType DatabaseType { get; }

        /// <summary>
        /// Gets the connection string for this database
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Tests the database connection
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if connection is successful</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets database health status
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Database health information</returns>
        Task<DatabaseHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a query and returns results
        /// </summary>
        /// <typeparam name="T">Type of result</typeparam>
        /// <param name="query">Query to execute</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Query results</returns>
        Task<IEnumerable<T>> QueryAsync<T>(string query, IDictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a command (INSERT, UPDATE, DELETE)
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="parameters">Command parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected rows</returns>
        Task<int> ExecuteAsync(string command, IDictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a database transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Database transaction</returns>
        Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets database statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Database statistics</returns>
        Task<DatabaseStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs database maintenance operations
        /// </summary>
        /// <param name="maintenanceType">Type of maintenance to perform</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Maintenance result</returns>
        Task<DatabaseMaintenanceResult> PerformMaintenanceAsync(DatabaseMaintenanceType maintenanceType, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Database transaction interface
    /// </summary>
    public interface IDatabaseTransaction : IDisposable
    {
        /// <summary>
        /// Commits the transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Database types supported by the framework
    /// </summary>
    public enum DatabaseType
    {
        SqlServer,
        PostgreSQL,
        MongoDB,
        SQLite,
        MySQL,
        Oracle
    }

    /// <summary>
    /// Database health status
    /// </summary>
    public class DatabaseHealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Database statistics
    /// </summary>
    public class DatabaseStatistics
    {
        public long TotalConnections { get; set; }
        public long ActiveConnections { get; set; }
        public long TotalQueries { get; set; }
        public long FailedQueries { get; set; }
        public TimeSpan AverageQueryTime { get; set; }
        public long TotalTransactions { get; set; }
        public long FailedTransactions { get; set; }
        public DateTime LastReset { get; set; }
    }

    /// <summary>
    /// Database maintenance types
    /// </summary>
    public enum DatabaseMaintenanceType
    {
        Backup,
        Restore,
        Optimize,
        Cleanup,
        Reindex,
        Vacuum
    }

    /// <summary>
    /// Database maintenance result
    /// </summary>
    public class DatabaseMaintenanceResult
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime CompletedAt { get; set; }
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }
} 