using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Validation functionality for MigrationService.
    /// Handles migration validation, dependency checking, and conflict resolution.
    /// </summary>
    public partial class MigrationService
    {
        /// <summary>
        /// Validates all migrations for consistency and dependencies.
        /// </summary>
        public async Task<MigrationValidationResult> ValidateMigrationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await EnsureMigrationTablesExistAsync(cancellationToken);
                await LoadMigrationStateFromDatabaseAsync(cancellationToken);
                
                var errors = new List<string>();
                var warnings = new List<string>();
                var pendingMigrations = _migrations.Values.Where(m => !m.IsApplied).ToList();
                var appliedMigrations = _migrations.Values.Where(m => m.IsApplied).ToList();

                // Check for circular dependencies
                foreach (var migration in pendingMigrations)
                {
                    if (HasCircularDependency(migration, _migrations.Values))
                    {
                        errors.Add($"Circular dependency detected in migration {migration.Id}");
                    }
                }

                // Check for missing dependencies
                foreach (var migration in pendingMigrations)
                {
                    foreach (var dependency in migration.Dependencies)
                    {
                        if (!_migrations.ContainsKey(dependency))
                        {
                            errors.Add($"Migration {migration.Id} depends on non-existent migration {dependency}");
                        }
                        else if (!_migrations[dependency].IsApplied)
                        {
                            warnings.Add($"Migration {migration.Id} depends on unapplied migration {dependency}");
                        }
                    }
                }

                // Check for version conflicts
                var versionGroups = pendingMigrations.GroupBy(m => m.Version).Where(g => g.Count() > 1);
                foreach (var group in versionGroups)
                {
                    warnings.Add($"Multiple migrations with version {group.Key}: {string.Join(", ", group.Select(m => m.Id))}");
                }

                var currentVersion = appliedMigrations.Any() ? appliedMigrations.Max(m => m.Version) : "0.0.0";
                var targetVersion = pendingMigrations.Any() ? pendingMigrations.Max(m => m.Version) : currentVersion;

                return new MigrationValidationResult
                {
                    IsValid = errors.Count == 0,
                    Errors = errors,
                    Warnings = warnings,
                    PendingMigrations = pendingMigrations,
                    CurrentVersion = currentVersion,
                    TargetVersion = targetVersion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate migrations");
                return new MigrationValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { $"Validation failed: {ex.Message}" },
                    CurrentVersion = "0.0.0",
                    TargetVersion = "0.0.0"
                };
            }
        }

        /// <summary>
        /// Creates a new migration.
        /// </summary>
        public async Task<MigrationInfo> CreateMigrationAsync(string name, string description, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("Migration name cannot be empty", nameof(name));
                }

                var timestamp = DateTime.UtcNow;
                var version = $"{timestamp:yyyy.MM.dd.HHmmss}";
                var id = $"{version}_{name.Replace(" ", "_")}";
                
                var migration = new MigrationInfo
                {
                    Id = id,
                    Name = name,
                    Description = description,
                    Version = version,
                    Timestamp = timestamp,
                    IsApplied = false,
                    Script = $"-- Migration: {name}\n-- Description: {description}\n-- Version: {version}\n-- Generated: {timestamp:yyyy-MM-dd HH:mm:ss}\n\n-- TODO: Add your migration SQL here\n",
                    RollbackScript = $"-- Rollback: {name}\n-- Description: {description}\n-- Version: {version}\n-- Generated: {timestamp:yyyy-MM-dd HH:mm:ss}\n\n-- TODO: Add your rollback SQL here\n"
                };

                _migrations[id] = migration;
                _logger.LogInformation("Created new migration: {MigrationId}", id);
                
                return migration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create migration: {Name}", name);
                throw;
            }
        }

        /// <summary>
        /// Checks for circular dependencies in migration graph.
        /// </summary>
        private bool HasCircularDependency(MigrationInfo migration, IEnumerable<MigrationInfo> allMigrations)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();
            return HasCircularDependencyRecursive(migration.Id, visited, recursionStack, allMigrations);
        }

        /// <summary>
        /// Recursively checks for circular dependencies.
        /// </summary>
        private bool HasCircularDependencyRecursive(string migrationId, HashSet<string> visited, HashSet<string> recursionStack, IEnumerable<MigrationInfo> allMigrations)
        {
            if (recursionStack.Contains(migrationId))
                return true;

            if (visited.Contains(migrationId))
                return false;

            visited.Add(migrationId);
            recursionStack.Add(migrationId);

            var migration = allMigrations.FirstOrDefault(m => m.Id == migrationId);
            if (migration != null)
            {
                foreach (var dependency in migration.Dependencies)
                {
                    if (HasCircularDependencyRecursive(dependency, visited, recursionStack, allMigrations))
                        return true;
                }
            }

            recursionStack.Remove(migrationId);
            return false;
        }
    }
}
