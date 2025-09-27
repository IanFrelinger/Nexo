using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// History and versioning functionality for MigrationService.
    /// Handles migration history, versioning, and schema version management.
    /// </summary>
    public partial class MigrationService
    {
        /// <summary>
        /// Gets migration history.
        /// </summary>
        public async Task<IEnumerable<MigrationHistory>> GetMigrationHistoryAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureMigrationTablesExistAsync(cancellationToken);
                
                var query = $"SELECT * FROM {_migrationHistoryTableName} ORDER BY AppliedAt DESC";
                var results = await _databaseProvider.QueryAsync<MigrationHistory>(query, null, cancellationToken);
                
                return results ?? new List<MigrationHistory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get migration history");
                return new List<MigrationHistory>();
            }
        }

        /// <summary>
        /// Gets current schema version.
        /// </summary>
        public async Task<string> GetSchemaVersionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureMigrationTablesExistAsync(cancellationToken);
                await LoadMigrationStateFromDatabaseAsync(cancellationToken);
                
                var appliedMigrations = _migrations.Values.Where(m => m.IsApplied);
                return appliedMigrations.Any() ? appliedMigrations.Max(m => m.Version) : "0.0.0";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get schema version");
                return "0.0.0";
            }
        }
    }
}
