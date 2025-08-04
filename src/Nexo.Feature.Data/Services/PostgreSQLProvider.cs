using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// PostgreSQL database provider implementation
    /// </summary>
    public class PostgreSQLProvider : IDatabaseProvider
    {
        private readonly ILogger<PostgreSQLProvider> _logger;
        private readonly string _connectionString;
        private readonly object _lockObject = new object();
        private long _totalConnections;
        private long _activeConnections;
        private long _totalQueries;
        private long _failedQueries;
        private long _totalTransactions;
        private long _failedTransactions;
        private readonly List<TimeSpan> _queryTimes = new List<TimeSpan>();

        public PostgreSQLProvider(ILogger<PostgreSQLProvider> logger, string connectionString)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public DatabaseType DatabaseType => DatabaseType.PostgreSQL;
        public string ConnectionString => _connectionString;

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL connection test failed");
                return false;
            }
        }

        public async Task<DatabaseHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = new Npgsql.NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(cancellationToken);

                stopwatch.Stop();
                return new DatabaseHealthStatus
                {
                    IsHealthy = true,
                    Status = "Healthy",
                    LastChecked = DateTime.UtcNow,
                    ResponseTime = stopwatch.Elapsed,
                    ErrorMessage = string.Empty,
                    Details = new Dictionary<string, object>
                    {
                        ["DatabaseType"] = "PostgreSQL",
                        ["ServerVersion"] = connection.ServerVersion
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "PostgreSQL health check failed");
                return new DatabaseHealthStatus
                {
                    IsHealthy = false,
                    Status = "Unhealthy",
                    LastChecked = DateTime.UtcNow,
                    ResponseTime = stopwatch.Elapsed,
                    ErrorMessage = ex.Message,
                    Details = new Dictionary<string, object>
                    {
                        ["DatabaseType"] = "PostgreSQL",
                        ["Exception"] = ex.GetType().Name
                    }
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
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = new Npgsql.NpgsqlCommand(query, connection);
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
                _logger.LogError(ex, "PostgreSQL query execution failed: {Query}", query);
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
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var sqlCommand = new Npgsql.NpgsqlCommand(command, connection);
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
                _logger.LogError(ex, "PostgreSQL command execution failed: {Command}", command);
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
                var connection = new Npgsql.NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                var transaction = await connection.BeginTransactionAsync(cancellationToken) as Npgsql.NpgsqlTransaction;
                if (transaction == null)
                {
                    throw new InvalidOperationException("Failed to create PostgreSQL transaction");
                }
                return new PostgreSQLTransaction(transaction, connection, this);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedTransactions);
                _logger.LogError(ex, "Failed to begin PostgreSQL transaction");
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
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                string command = maintenanceType switch
                {
                    DatabaseMaintenanceType.Backup => "pg_dump @DatabaseName > @BackupPath",
                    DatabaseMaintenanceType.Restore => "psql @DatabaseName < @BackupPath",
                    DatabaseMaintenanceType.Optimize => "VACUUM ANALYZE",
                    DatabaseMaintenanceType.Cleanup => "VACUUM",
                    DatabaseMaintenanceType.Reindex => "REINDEX DATABASE @DatabaseName",
                    DatabaseMaintenanceType.Vacuum => "VACUUM FULL",
                    _ => throw new ArgumentException($"Unsupported maintenance type: {maintenanceType}")
                };

                using var sqlCommand = new Npgsql.NpgsqlCommand(command, connection);
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
                _logger.LogError(ex, "PostgreSQL maintenance operation failed: {MaintenanceType}", maintenanceType);
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
    /// PostgreSQL transaction implementation
    /// </summary>
    public class PostgreSQLTransaction : IDatabaseTransaction
    {
        private readonly Npgsql.NpgsqlTransaction _transaction;
        private readonly Npgsql.NpgsqlConnection _connection;
        private readonly PostgreSQLProvider _provider;
        private bool _disposed;

        public PostgreSQLTransaction(Npgsql.NpgsqlTransaction transaction, Npgsql.NpgsqlConnection connection, PostgreSQLProvider provider)
        {
            _transaction = transaction;
            _connection = connection;
            _provider = provider;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PostgreSQLTransaction));
            
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
            if (_disposed) throw new ObjectDisposedException(nameof(PostgreSQLTransaction));
            
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