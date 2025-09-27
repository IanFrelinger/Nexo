using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Rollback functionality for MigrationService.
    /// Handles migration rollback operations and version management.
    /// </summary>
    public partial class MigrationService
    {
        /// <summary>
        /// Rollbacks the last applied migration.
        /// </summary>
        public async Task<MigrationResult> RollbackLastMigrationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureMigrationTablesExistAsync(cancellationToken);
                var appliedMigrations = await GetAppliedMigrationsAsync(cancellationToken);
                var lastMigration = appliedMigrations.LastOrDefault();
                
                if (lastMigration == null)
                {
                    return new MigrationResult
                    {
                        IsSuccessful = true,
                        Message = "No migrations to rollback",
                        ExecutionTime = TimeSpan.Zero,
                        ExecutedAt = DateTime.UtcNow
                    };
                }

                return await RollbackToMigrationAsync(lastMigration.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rollback last migration");
                return new MigrationResult
                {
                    IsSuccessful = false,
                    Message = $"Failed to rollback last migration: {ex.Message}",
                    ExecutionTime = TimeSpan.Zero,
                    ExecutedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Rollbacks to a specific migration.
        /// </summary>
        public async Task<MigrationResult> RollbackToMigrationAsync(string migrationId, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var rolledBackMigrations = new List<MigrationInfo>();
            
            try
            {
                await EnsureMigrationTablesExistAsync(cancellationToken);
                var appliedMigrations = await GetAppliedMigrationsAsync(cancellationToken);
                
                // Get migrations to rollback (those applied after the target migration)
                var targetMigration = appliedMigrations.FirstOrDefault(m => m.Id == migrationId);
                if (targetMigration == null)
                {
                    throw new ArgumentException($"Migration {migrationId} not found or not applied");
                }

                var migrationsToRollback = appliedMigrations
                    .Where(m => m.Timestamp > targetMigration.Timestamp)
                    .OrderByDescending(m => m.Timestamp)
                    .ToList();

                foreach (var migration in migrationsToRollback)
                {
                    try
                    {
                        // Execute rollback script
                        if (!string.IsNullOrEmpty(migration.RollbackScript))
                        {
                            await _databaseProvider.ExecuteAsync(migration.RollbackScript, null, cancellationToken);
                        }

                        // Mark as not applied in database
                        await MarkMigrationAsNotAppliedAsync(migration, cancellationToken);
                        
                        // Update local state
                        migration.IsApplied = false;
                        rolledBackMigrations.Add(migration);
                        
                        _logger.LogInformation("Successfully rolled back migration: {MigrationId}", migration.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to rollback migration: {MigrationId}", migration.Id);
                        throw;
                    }
                }

                stopwatch.Stop();
                return new MigrationResult
                {
                    IsSuccessful = true,
                    Message = $"Successfully rolled back {rolledBackMigrations.Count} migrations to {migrationId}",
                    AppliedMigrations = rolledBackMigrations,
                    ExecutionTime = stopwatch.Elapsed,
                    ExecutedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to rollback to migration {MigrationId}", migrationId);
                return new MigrationResult
                {
                    IsSuccessful = false,
                    Message = $"Failed to rollback to migration {migrationId}: {ex.Message}",
                    FailedMigrations = rolledBackMigrations,
                    ExecutionTime = stopwatch.Elapsed,
                    ExecutedAt = DateTime.UtcNow
                };
            }
        }
    }
}
