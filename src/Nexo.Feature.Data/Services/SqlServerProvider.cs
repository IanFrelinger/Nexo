using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// SQL Server database provider implementation
    /// </summary>
    public class SqlServerProvider : IDatabaseProvider
    {
        private readonly ILogger<SqlServerProvider> _logger;
        private readonly string _connectionString;
        private readonly object _lockObject = new object();
        private long _totalConnections;
        private long _activeConnections;
        private long _totalQueries;
        private long _failedQueries;
        private long _totalTransactions;
        private long _failedTransactions;
        private readonly List<TimeSpan> _queryTimes = new List<TimeSpan>();

        public SqlServerProvider(ILogger<SqlServerProvider> logger, string connectionString)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public DatabaseType DatabaseType => DatabaseType.SqlServer;
        public string ConnectionString => _connectionString;

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test SQL Server connection");
                return false;
            }
        }

        public async Task<DatabaseHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                // Test basic connectivity
                using var command = new Microsoft.Data.SqlClient.SqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(cancellationToken);

                stopwatch.Stop();
                return new DatabaseHealthStatus
                {
                    IsHealthy = true,
                    Status = "Healthy",
                    LastChecked = DateTime.UtcNow,
                    ResponseTime = stopwatch.Elapsed,
                    Details = new Dictionary<string, object>
                    {
                        ["DatabaseType"] = DatabaseType.ToString(),
                        ["ServerVersion"] = connection.ServerVersion,
                        ["Database"] = connection.Database
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Database health check failed");
                return new DatabaseHealthStatus
                {
                    IsHealthy = false,
                    Status = "Unhealthy",
                    LastChecked = DateTime.UtcNow,
                    ResponseTime = stopwatch.Elapsed,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string query, IDictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref _totalConnections);
            Interlocked.Increment(ref _activeConnections);

            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                var results = new List<T>();
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                
                while (await reader.ReadAsync(cancellationToken))
                {
                    // Simple mapping - in a real implementation, you'd use a proper ORM or mapping library
                    if (typeof(T) == typeof(object))
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        results.Add((T)(object)row);
                    }
                }

                stopwatch.Stop();
                UpdateQueryMetrics(stopwatch.Elapsed, true);
                return results;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                UpdateQueryMetrics(stopwatch.Elapsed, false);
                _logger.LogError(ex, "Query execution failed: {Query}", query);
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref _activeConnections);
            }
        }

        public async Task<int> ExecuteAsync(string command, IDictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            Interlocked.Increment(ref _totalConnections);
            Interlocked.Increment(ref _activeConnections);

            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var sqlCommand = new Microsoft.Data.SqlClient.SqlCommand(command, connection);
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        sqlCommand.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                    }
                }

                var result = await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
                stopwatch.Stop();
                UpdateQueryMetrics(stopwatch.Elapsed, true);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                UpdateQueryMetrics(stopwatch.Elapsed, false);
                _logger.LogError(ex, "Command execution failed: {Command}", command);
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref _activeConnections);
            }
        }

        public async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _totalTransactions);
            try
            {
                var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                var transaction = await connection.BeginTransactionAsync(cancellationToken) as Microsoft.Data.SqlClient.SqlTransaction;
                if (transaction == null)
                {
                    throw new InvalidOperationException("Failed to create SQL Server transaction");
                }
                return new SqlServerTransaction(transaction, connection, this);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedTransactions);
                _logger.LogError(ex, "Failed to begin transaction");
                throw;
            }
        }

        public async Task<DatabaseStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                var averageQueryTime = _queryTimes.Count > 0 
                    ? TimeSpan.FromTicks((long)_queryTimes.Average(t => t.Ticks))
                    : TimeSpan.Zero;

                return new DatabaseStatistics
                {
                    TotalConnections = _totalConnections,
                    ActiveConnections = _activeConnections,
                    TotalQueries = _totalQueries,
                    FailedQueries = _failedQueries,
                    AverageQueryTime = averageQueryTime,
                    TotalTransactions = _totalTransactions,
                    FailedTransactions = _failedTransactions,
                    LastReset = DateTime.UtcNow
                };
            }
        }

        public async Task<DatabaseMaintenanceResult> PerformMaintenanceAsync(DatabaseMaintenanceType maintenanceType, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                string command = maintenanceType switch
                {
                    DatabaseMaintenanceType.Backup => "BACKUP DATABASE @DatabaseName TO DISK = @BackupPath",
                    DatabaseMaintenanceType.Restore => "RESTORE DATABASE @DatabaseName FROM DISK = @BackupPath",
                    DatabaseMaintenanceType.Optimize => "UPDATE STATISTICS ALL",
                    DatabaseMaintenanceType.Cleanup => "DBCC SHRINKDATABASE(@DatabaseName)",
                    DatabaseMaintenanceType.Reindex => "EXEC sp_MSforeachtable 'ALTER INDEX ALL ON ? REBUILD'",
                    DatabaseMaintenanceType.Vacuum => "DBCC SHRINKDATABASE(@DatabaseName)",
                    _ => throw new ArgumentException($"Unsupported maintenance type: {maintenanceType}")
                };

                using var sqlCommand = new Microsoft.Data.SqlClient.SqlCommand(command, connection);
                await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

                stopwatch.Stop();
                return new DatabaseMaintenanceResult
                {
                    IsSuccessful = true,
                    Message = $"Successfully performed {maintenanceType} maintenance",
                    Duration = stopwatch.Elapsed,
                    CompletedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Maintenance operation failed: {MaintenanceType}", maintenanceType);
                return new DatabaseMaintenanceResult
                {
                    IsSuccessful = false,
                    Message = $"Failed to perform {maintenanceType} maintenance: {ex.Message}",
                    Duration = stopwatch.Elapsed,
                    CompletedAt = DateTime.UtcNow
                };
            }
        }

        private void UpdateQueryMetrics(TimeSpan queryTime, bool isSuccess)
        {
            lock (_lockObject)
            {
                Interlocked.Increment(ref _totalQueries);
                if (!isSuccess)
                {
                    Interlocked.Increment(ref _failedQueries);
                }
                _queryTimes.Add(queryTime);
                
                // Keep only last 1000 query times to prevent memory growth
                if (_queryTimes.Count > 1000)
                {
                    _queryTimes.RemoveAt(0);
                }
            }
        }

        internal void OnTransactionCompleted(bool isSuccess)
        {
            if (!isSuccess)
            {
                Interlocked.Increment(ref _failedTransactions);
            }
        }
    }

    /// <summary>
    /// SQL Server transaction implementation
    /// </summary>
    public class SqlServerTransaction : IDatabaseTransaction
    {
        private readonly Microsoft.Data.SqlClient.SqlTransaction _transaction;
        private readonly Microsoft.Data.SqlClient.SqlConnection _connection;
        private readonly SqlServerProvider _provider;
        private bool _disposed;

        public SqlServerTransaction(Microsoft.Data.SqlClient.SqlTransaction transaction, Microsoft.Data.SqlClient.SqlConnection connection, SqlServerProvider provider)
        {
            _transaction = transaction;
            _connection = connection;
            _provider = provider;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SqlServerTransaction));
            
            try
            {
                await _transaction.CommitAsync(cancellationToken);
                _provider.OnTransactionCompleted(true);
            }
            catch
            {
                _provider.OnTransactionCompleted(false);
                throw;
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SqlServerTransaction));
            
            try
            {
                await _transaction.RollbackAsync(cancellationToken);
                _provider.OnTransactionCompleted(true);
            }
            catch
            {
                _provider.OnTransactionCompleted(false);
                throw;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }
} 