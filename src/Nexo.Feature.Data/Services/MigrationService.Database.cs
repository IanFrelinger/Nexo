using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Database operations functionality for MigrationService.
    /// Handles database table management, state loading, and migration tracking.
    /// </summary>
    public partial class MigrationService
    {
        /// <summary>
        /// Ensures migration tables exist in the database.
        /// </summary>
        private async Task EnsureMigrationTablesExistAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Create migrations table
                var createMigrationsTable = $@"
                    CREATE TABLE IF NOT EXISTS {_migrationsTableName} (
                        Id VARCHAR(255) PRIMARY KEY,
                        Name VARCHAR(255) NOT NULL,
                        Description TEXT,
                        Version VARCHAR(50) NOT NULL,
                        Timestamp TIMESTAMP NOT NULL,
                        IsApplied BOOLEAN NOT NULL DEFAULT FALSE,
                        Script TEXT,
                        RollbackScript TEXT,
                        Dependencies TEXT,
                        Metadata TEXT,
                        AppliedAt TIMESTAMP,
                        AppliedBy VARCHAR(255)
                    )";

                await _databaseProvider.ExecuteAsync(createMigrationsTable, null, cancellationToken);

                // Create migration history table
                var createHistoryTable = $@"
                    CREATE TABLE IF NOT EXISTS {_migrationHistoryTableName} (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        MigrationId VARCHAR(255) NOT NULL,
                        MigrationName VARCHAR(255) NOT NULL,
                        AppliedAt TIMESTAMP NOT NULL,
                        AppliedBy VARCHAR(255),
                        ExecutionTime BIGINT,
                        WasSuccessful BOOLEAN NOT NULL,
                        ErrorMessage TEXT
                    )";

                await _databaseProvider.ExecuteAsync(createHistoryTable, null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure migration tables exist");
                throw;
            }
        }

        /// <summary>
        /// Loads migration state from database.
        /// </summary>
        private async Task LoadMigrationStateFromDatabaseAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var query = $"SELECT * FROM {_migrationsTableName}";
                var results = await _databaseProvider.QueryAsync<MigrationInfo>(query, null, cancellationToken);
                
                if (results != null)
                {
                    foreach (var migration in results)
                    {
                        if (_migrations.ContainsKey(migration.Id))
                        {
                            _migrations[migration.Id].IsApplied = migration.IsApplied;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load migration state from database");
                // Continue with in-memory state if database loading fails
            }
        }

        /// <summary>
        /// Marks a migration as applied in the database.
        /// </summary>
        private async Task MarkMigrationAsAppliedAsync(MigrationInfo migration, CancellationToken cancellationToken = default)
        {
            try
            {
                // Insert or update migration record
                var upsertQuery = $@"
                    INSERT OR REPLACE INTO {_migrationsTableName} 
                    (Id, Name, Description, Version, Timestamp, IsApplied, Script, RollbackScript, Dependencies, Metadata, AppliedAt, AppliedBy)
                    VALUES (@Id, @Name, @Description, @Version, @Timestamp, @IsApplied, @Script, @RollbackScript, @Dependencies, @Metadata, @AppliedAt, @AppliedBy)";

                var parameters = new Dictionary<string, object>
                {
                    ["Id"] = migration.Id,
                    ["Name"] = migration.Name,
                    ["Description"] = migration.Description,
                    ["Version"] = migration.Version,
                    ["Timestamp"] = migration.Timestamp,
                    ["IsApplied"] = true,
                    ["Script"] = migration.Script,
                    ["RollbackScript"] = migration.RollbackScript,
                    ["Dependencies"] = string.Join(",", migration.Dependencies),
                    ["Metadata"] = System.Text.Json.JsonSerializer.Serialize(migration.Metadata),
                    ["AppliedAt"] = DateTime.UtcNow,
                    ["AppliedBy"] = Environment.UserName
                };

                await _databaseProvider.ExecuteAsync(upsertQuery, parameters, cancellationToken);

                // Add to history
                var historyQuery = $@"
                    INSERT INTO {_migrationHistoryTableName} 
                    (MigrationId, MigrationName, AppliedAt, AppliedBy, ExecutionTime, WasSuccessful, ErrorMessage)
                    VALUES (@MigrationId, @MigrationName, @AppliedAt, @AppliedBy, @ExecutionTime, @WasSuccessful, @ErrorMessage)";

                var historyParameters = new Dictionary<string, object>
                {
                    ["MigrationId"] = migration.Id,
                    ["MigrationName"] = migration.Name,
                    ["AppliedAt"] = DateTime.UtcNow,
                    ["AppliedBy"] = Environment.UserName,
                    ["ExecutionTime"] = 0,
                    ["WasSuccessful"] = true,
                    ["ErrorMessage"] = ""
                };

                await _databaseProvider.ExecuteAsync(historyQuery, historyParameters, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark migration as applied: {MigrationId}", migration.Id);
                throw;
            }
        }

        /// <summary>
        /// Marks a migration as not applied in the database.
        /// </summary>
        private async Task MarkMigrationAsNotAppliedAsync(MigrationInfo migration, CancellationToken cancellationToken = default)
        {
            try
            {
                // Update migration record
                var updateQuery = $@"
                    UPDATE {_migrationsTableName} 
                    SET IsApplied = @IsApplied, AppliedAt = NULL, AppliedBy = NULL
                    WHERE Id = @Id";

                var parameters = new Dictionary<string, object>
                {
                    ["Id"] = migration.Id,
                    ["IsApplied"] = false
                };

                await _databaseProvider.ExecuteAsync(updateQuery, parameters, cancellationToken);

                // Add to history
                var historyQuery = $@"
                    INSERT INTO {_migrationHistoryTableName} 
                    (MigrationId, MigrationName, AppliedAt, AppliedBy, ExecutionTime, WasSuccessful, ErrorMessage)
                    VALUES (@MigrationId, @MigrationName, @AppliedAt, @AppliedBy, @ExecutionTime, @WasSuccessful, @ErrorMessage)";

                var historyParameters = new Dictionary<string, object>
                {
                    ["MigrationId"] = migration.Id,
                    ["MigrationName"] = migration.Name,
                    ["AppliedAt"] = DateTime.UtcNow,
                    ["AppliedBy"] = Environment.UserName,
                    ["ExecutionTime"] = 0,
                    ["WasSuccessful"] = true,
                    ["ErrorMessage"] = "Rollback"
                };

                await _databaseProvider.ExecuteAsync(historyQuery, historyParameters, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark migration as not applied: {MigrationId}", migration.Id);
                throw;
            }
        }
    }
}
