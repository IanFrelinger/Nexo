using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// MongoDB database provider implementation
    /// </summary>
    public class MongoDBProvider : IDatabaseProvider
    {
        private readonly ILogger<MongoDBProvider> _logger;
        private readonly string _connectionString;
        private readonly string _databaseName;
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly object _lockObject = new object();
        private long _totalConnections;
        private long _activeConnections;
        private long _totalQueries;
        private long _failedQueries;
        private long _totalTransactions;
        private long _failedTransactions;
        private readonly List<TimeSpan> _queryTimes = new List<TimeSpan>();

        public MongoDBProvider(ILogger<MongoDBProvider> logger, string connectionString, string databaseName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(databaseName);
        }

        public DatabaseType DatabaseType => DatabaseType.MongoDB;
        public string ConnectionString => _connectionString;

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _database.RunCommandAsync<BsonDocument>("{ping: 1}", null, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDB connection test failed");
                return false;
            }
        }

        public async Task<DatabaseHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _database.RunCommandAsync<BsonDocument>("{ping: 1}", null, cancellationToken);
                
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
                        ["DatabaseType"] = "MongoDB",
                        ["DatabaseName"] = _databaseName,
                        ["ServerVersion"] = await GetServerVersionAsync(cancellationToken)
                    }
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "MongoDB health check failed");
                return new DatabaseHealthStatus
                {
                    IsHealthy = false,
                    Status = "Unhealthy",
                    LastChecked = DateTime.UtcNow,
                    ResponseTime = stopwatch.Elapsed,
                    ErrorMessage = ex.Message,
                    Details = new Dictionary<string, object>
                    {
                        ["DatabaseType"] = "MongoDB",
                        ["DatabaseName"] = _databaseName,
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
                // For MongoDB, we'll use a simple collection-based approach
                // In a real implementation, you'd parse the query and convert it to MongoDB operations
                var collectionName = ExtractCollectionName(query);
                var collection = _database.GetCollection<BsonDocument>(collectionName);
                
                var filter = Builders<BsonDocument>.Filter.Empty;
                if (parameters != null && parameters.Count > 0)
                {
                    // Simple parameter substitution - in a real implementation, you'd use proper MongoDB query building
                    var filterBuilder = Builders<BsonDocument>.Filter;
                    var filters = new List<FilterDefinition<BsonDocument>>();
                    
                    foreach (var param in parameters)
                    {
                        filters.Add(filterBuilder.Eq(param.Key, BsonValue.Create(param.Value)));
                    }
                    
                    if (filters.Count > 0)
                    {
                        filter = filterBuilder.And(filters);
                    }
                }

                var documents = await collection.Find(filter).ToListAsync(cancellationToken);
                
                var results = new List<T>();
                foreach (var doc in documents)
                {
                    if (typeof(T) == typeof(object))
                    {
                        var dict = doc.ToDictionary();
                        results.Add((T)(object)dict);
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
                _logger.LogError(ex, "MongoDB query execution failed: {Query}", query);
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
                // For MongoDB, we'll handle basic operations like insert, update, delete
                var operation = ExtractOperation(command);
                var collectionName = ExtractCollectionName(command);
                var collection = _database.GetCollection<BsonDocument>(collectionName);

                int affectedRows = 0;

                switch (operation.ToLower())
                {
                    case "insert":
                        if (parameters != null && parameters.ContainsKey("document"))
                        {
                            var document = BsonDocument.Parse(parameters["document"].ToString()!);
                            await collection.InsertOneAsync(document, null, cancellationToken);
                            affectedRows = 1;
                        }
                        break;

                    case "update":
                        if (parameters != null)
                        {
                            var id = parameters.ContainsKey("id") ? parameters["id"] : null;
                            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                            var update = Builders<BsonDocument>.Update.Set("updated", DateTime.UtcNow);
                            var result = await collection.UpdateOneAsync(filter, update, null, cancellationToken);
                            affectedRows = (int)result.ModifiedCount;
                        }
                        break;

                    case "delete":
                        if (parameters != null)
                        {
                            var id = parameters.ContainsKey("id") ? parameters["id"] : null;
                            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                            var result = await collection.DeleteOneAsync(filter, cancellationToken);
                            affectedRows = (int)result.DeletedCount;
                        }
                        break;

                    default:
                        throw new NotSupportedException($"MongoDB operation '{operation}' is not supported");
                }

                stopwatch.Stop();
                UpdateQueryMetrics(stopwatch.Elapsed, true);
                return affectedRows;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                UpdateQueryMetrics(stopwatch.Elapsed, false);
                _logger.LogError(ex, "MongoDB command execution failed: {Command}", command);
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
                var session = await _client.StartSessionAsync(null, cancellationToken);
                session.StartTransaction();
                return new MongoDBTransaction(session, this);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedTransactions);
                _logger.LogError(ex, "Failed to begin MongoDB transaction");
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
                string command = maintenanceType switch
                {
                    DatabaseMaintenanceType.Backup => "mongodump",
                    DatabaseMaintenanceType.Restore => "mongorestore",
                    DatabaseMaintenanceType.Optimize => "db.repairDatabase()",
                    DatabaseMaintenanceType.Cleanup => "db.cleanup()",
                    DatabaseMaintenanceType.Reindex => "db.reIndex()",
                    DatabaseMaintenanceType.Vacuum => "db.repairDatabase()",
                    _ => throw new ArgumentException($"Unsupported maintenance type: {maintenanceType}")
                };

                // For MongoDB, we'll execute the command directly
                var result = await _database.RunCommandAsync<BsonDocument>(command, null, cancellationToken);

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
                _logger.LogError(ex, "MongoDB maintenance operation failed: {MaintenanceType}", maintenanceType);
                return new DatabaseMaintenanceResult
                {
                    IsSuccessful = false,
                    Message = $"Failed to perform {maintenanceType} maintenance: {ex.Message}",
                    Duration = stopwatch.Elapsed,
                    CompletedAt = DateTime.UtcNow
                };
            }
        }

        private async Task<string> GetServerVersionAsync(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _database.RunCommandAsync<BsonDocument>("{buildInfo: 1}", null, cancellationToken);
                return result.GetValue("version", "Unknown").AsString;
            }
            catch
            {
                return "Unknown";
            }
        }

        private string ExtractCollectionName(string query)
        {
            // Simple extraction - in a real implementation, you'd use proper parsing
            if (query.Contains("FROM"))
            {
                var parts = query.Split(new[] { "FROM" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    return parts[1].Trim().Split(' ')[0].Trim();
                }
            }
            return "default";
        }

        private string ExtractOperation(string command)
        {
            // Simple extraction - in a real implementation, you'd use proper parsing
            var parts = command.Trim().Split(' ');
            return parts[0].ToUpper();
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
    /// MongoDB transaction implementation
    /// </summary>
    public class MongoDBTransaction : IDatabaseTransaction
    {
        private readonly IClientSessionHandle _session;
        private readonly MongoDBProvider _provider;
        private bool _disposed;

        public MongoDBTransaction(IClientSessionHandle session, MongoDBProvider provider)
        {
            _session = session;
            _provider = provider;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MongoDBTransaction));
            
            try
            {
                await _session.CommitTransactionAsync(cancellationToken);
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
            if (_disposed) throw new ObjectDisposedException(nameof(MongoDBTransaction));
            
            try
            {
                await _session.AbortTransactionAsync(cancellationToken);
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
                _session?.Dispose();
                _disposed = true;
            }
        }
    }
} 