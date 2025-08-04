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
    /// Enhanced database migration service implementation with versioning and rollback support
    /// </summary>
    public class MigrationService : IMigrationService
    {
        private readonly IDatabaseProvider _databaseProvider;
        private readonly ILogger<MigrationService> _logger;
        private readonly Dictionary<string, MigrationInfo> _migrations = new Dictionary<string, MigrationInfo>();
        private readonly string _migrationsTableName = "__Migrations";
        private readonly string _migrationHistoryTableName = "__MigrationHistory";

        public MigrationService(IDatabaseProvider databaseProvider, ILogger<MigrationService> logger)
        {
            _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize with some sample migrations
            InitializeSampleMigrations();
        }

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

        private void InitializeSampleMigrations()
        {
            var migration1 = new MigrationInfo
            {
                Id = "2025.01.26.100000_Initial_Schema",
                Name = "Initial Schema",
                Description = "Create initial database schema",
                Version = "2025.01.26.100000",
                Timestamp = new DateTime(2025, 1, 26, 10, 0, 0, DateTimeKind.Utc),
                IsApplied = false,
                Script = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username VARCHAR(100) NOT NULL UNIQUE,
                        Email VARCHAR(255) NOT NULL UNIQUE,
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );
                    
                    CREATE TABLE IF NOT EXISTS Projects (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name VARCHAR(255) NOT NULL,
                        Description TEXT,
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );",
                RollbackScript = @"
                    DROP TABLE IF EXISTS Projects;
                    DROP TABLE IF EXISTS Users;",
                Dependencies = new List<string>()
            };

            var migration2 = new MigrationInfo
            {
                Id = "2025.01.26.110000_Add_User_Roles",
                Name = "Add User Roles",
                Description = "Add role-based access control",
                Version = "2025.01.26.110000",
                Timestamp = new DateTime(2025, 1, 26, 11, 0, 0, DateTimeKind.Utc),
                IsApplied = false,
                Script = @"
                    ALTER TABLE Users ADD COLUMN Role VARCHAR(50) DEFAULT 'User';
                    CREATE TABLE IF NOT EXISTS Roles (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name VARCHAR(50) NOT NULL UNIQUE,
                        Description TEXT
                    );
                    
                    INSERT INTO Roles (Name, Description) VALUES 
                        ('Admin', 'Administrator with full access'),
                        ('User', 'Standard user with limited access'),
                        ('Guest', 'Guest user with read-only access');",
                RollbackScript = @"
                    DROP TABLE IF EXISTS Roles;
                    ALTER TABLE Users DROP COLUMN Role;",
                Dependencies = new List<string> { "2025.01.26.100000_Initial_Schema" }
            };

            _migrations[migration1.Id] = migration1;
            _migrations[migration2.Id] = migration2;
        }

        private bool HasCircularDependency(MigrationInfo migration, IEnumerable<MigrationInfo> allMigrations)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();
            return HasCircularDependencyRecursive(migration.Id, visited, recursionStack, allMigrations);
        }

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