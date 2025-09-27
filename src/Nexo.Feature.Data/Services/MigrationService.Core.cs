using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Core migration operations for MigrationService.
    /// Handles main migration operations including apply, rollback, and retrieval.
    /// </summary>
    public partial class MigrationService
    {
        /// <summary>
        /// Gets all migrations.
        /// </summary>
        public async Task<IEnumerable<MigrationInfo>> GetMigrationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureMigrationTablesExistAsync(cancellationToken);
                await LoadMigrationStateFromDatabaseAsync(cancellationToken);
                return _migrations.Values.OrderBy(m => m.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get migrations");
                throw;
            }
        }

        /// <summary>
        /// Gets applied migrations.
        /// </summary>
        public async Task<IEnumerable<MigrationInfo>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureMigrationTablesExistAsync(cancellationToken);
                await LoadMigrationStateFromDatabaseAsync(cancellationToken);
                return _migrations.Values.Where(m => m.IsApplied).OrderBy(m => m.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get applied migrations");
                throw;
            }
        }

        /// <summary>
        /// Gets pending migrations.
        /// </summary>
        public async Task<IEnumerable<MigrationInfo>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureMigrationTablesExistAsync(cancellationToken);
                await LoadMigrationStateFromDatabaseAsync(cancellationToken);
                return _migrations.Values.Where(m => !m.IsApplied).OrderBy(m => m.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pending migrations");
                throw;
            }
        }

        /// <summary>
        /// Applies all pending migrations.
        /// </summary>
        public async Task<MigrationResult> ApplyMigrationsAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var appliedMigrations = new List<MigrationInfo>();
            var failedMigrations = new List<MigrationInfo>();

            try
            {
                await EnsureMigrationTablesExistAsync(cancellationToken);
                var pendingMigrations = await GetPendingMigrationsAsync(cancellationToken);
                
                foreach (var migration in pendingMigrations)
                {
                    try
                    {
                        var result = await ApplyMigrationAsync(migration.Id, cancellationToken);
                        if (result.IsSuccessful)
                        {
                            appliedMigrations.Add(migration);
                            _logger.LogInformation("Successfully applied migration: {MigrationId}", migration.Id);
                        }
                        else
                        {
                            failedMigrations.Add(migration);
                            _logger.LogError("Failed to apply migration: {MigrationId} - {Message}", migration.Id, result.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedMigrations.Add(migration);
                        _logger.LogError(ex, "Failed to apply migration: {MigrationId}", migration.Id);
                    }
                }

                stopwatch.Stop();
                return new MigrationResult
                {
                    IsSuccessful = failedMigrations.Count == 0,
                    Message = $"Applied {appliedMigrations.Count} migrations, {failedMigrations.Count} failed",
                    AppliedMigrations = appliedMigrations,
                    FailedMigrations = failedMigrations,
                    ExecutionTime = stopwatch.Elapsed,
                    ExecutedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to apply migrations");
                return new MigrationResult
                {
                    IsSuccessful = false,
                    Message = $"Failed to apply migrations: {ex.Message}",
                    FailedMigrations = failedMigrations,
                    ExecutionTime = stopwatch.Elapsed,
                    ExecutedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Applies a specific migration.
        /// </summary>
        public async Task<MigrationResult> ApplyMigrationAsync(string migrationId, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                await EnsureMigrationTablesExistAsync(cancellationToken);
                
                if (!_migrations.TryGetValue(migrationId, out var migration))
                {
                    throw new ArgumentException($"Migration {migrationId} not found");
                }

                if (migration.IsApplied)
                {
                    _logger.LogWarning("Migration {MigrationId} is already applied", migrationId);
                    return new MigrationResult
                    {
                        IsSuccessful = true,
                        Message = $"Migration {migrationId} is already applied",
                        AppliedMigrations = new List<MigrationInfo> { migration },
                        ExecutionTime = stopwatch.Elapsed,
                        ExecutedAt = DateTime.UtcNow
                    };
                }

                // Check dependencies
                foreach (var dependency in migration.Dependencies)
                {
                    if (!_migrations.TryGetValue(dependency, out var depMigration) || !depMigration.IsApplied)
                    {
                        throw new InvalidOperationException($"Dependency {dependency} for migration {migrationId} is not applied");
                    }
                }

                // Execute migration script
                if (!string.IsNullOrEmpty(migration.Script))
                {
                    await _databaseProvider.ExecuteAsync(migration.Script, null, cancellationToken);
                }

                // Mark as applied in database
                await MarkMigrationAsAppliedAsync(migration, cancellationToken);
                
                // Update local state
                migration.IsApplied = true;
                
                stopwatch.Stop();
                return new MigrationResult
                {
                    IsSuccessful = true,
                    Message = $"Successfully applied migration {migrationId}",
                    AppliedMigrations = new List<MigrationInfo> { migration },
                    ExecutionTime = stopwatch.Elapsed,
                    ExecutedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to apply migration {MigrationId}", migrationId);
                return new MigrationResult
                {
                    IsSuccessful = false,
                    Message = $"Failed to apply migration {migrationId}: {ex.Message}",
                    FailedMigrations = new List<MigrationInfo>(),
                    ExecutionTime = stopwatch.Elapsed,
                    ExecutedAt = DateTime.UtcNow
                };
            }
        }
    }
}
