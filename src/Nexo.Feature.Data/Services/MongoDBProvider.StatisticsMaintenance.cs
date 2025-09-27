using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Statistics and maintenance functionality for MongoDB provider.
    /// </summary>
    public partial class MongoDBProvider
    {
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
    }
}
