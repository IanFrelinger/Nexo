using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Connection management functionality for MongoDB provider.
    /// </summary>
    public partial class MongoDBProvider
    {
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
    }
}
