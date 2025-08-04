using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Data.Interfaces
{
    /// <summary>
    /// Interface for database migration service
    /// </summary>
    public interface IMigrationService
    {
        /// <summary>
        /// Gets all available migrations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of available migrations</returns>
        Task<IEnumerable<MigrationInfo>> GetMigrationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets applied migrations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of applied migrations</returns>
        Task<IEnumerable<MigrationInfo>> GetAppliedMigrationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets pending migrations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of pending migrations</returns>
        Task<IEnumerable<MigrationInfo>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies all pending migrations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Migration result</returns>
        Task<MigrationResult> ApplyMigrationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies a specific migration
        /// </summary>
        /// <param name="migrationId">Migration ID to apply</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Migration result</returns>
        Task<MigrationResult> ApplyMigrationAsync(string migrationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the last migration
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Migration result</returns>
        Task<MigrationResult> RollbackLastMigrationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back to a specific migration
        /// </summary>
        /// <param name="migrationId">Migration ID to rollback to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Migration result</returns>
        Task<MigrationResult> RollbackToMigrationAsync(string migrationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates migrations without applying them
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<MigrationValidationResult> ValidateMigrationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new migration
        /// </summary>
        /// <param name="name">Migration name</param>
        /// <param name="description">Migration description</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created migration info</returns>
        Task<MigrationInfo> CreateMigrationAsync(string name, string description, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets migration history
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Migration history</returns>
        Task<IEnumerable<MigrationHistory>> GetMigrationHistoryAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets database schema version
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current schema version</returns>
        Task<string> GetSchemaVersionAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Migration information
    /// </summary>
    public class MigrationInfo
    {
        /// <summary>
        /// Gets or sets the migration ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the migration name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the migration description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the migration version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the migration timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets whether the migration is applied
        /// </summary>
        public bool IsApplied { get; set; }

        /// <summary>
        /// Gets or sets the migration script content
        /// </summary>
        public string Script { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the rollback script content
        /// </summary>
        public string RollbackScript { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets migration dependencies
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets migration metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Migration result
    /// </summary>
    public class MigrationResult
    {
        /// <summary>
        /// Gets or sets whether the migration was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the migration message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the applied migrations
        /// </summary>
        public List<MigrationInfo> AppliedMigrations { get; set; } = new List<MigrationInfo>();

        /// <summary>
        /// Gets or sets the failed migrations
        /// </summary>
        public List<MigrationInfo> FailedMigrations { get; set; } = new List<MigrationInfo>();

        /// <summary>
        /// Gets or sets the execution time
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the migration was executed
        /// </summary>
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// Gets or sets additional details
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Migration validation result
    /// </summary>
    public class MigrationValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation was successful
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets validation warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the pending migrations that would be applied
        /// </summary>
        public List<MigrationInfo> PendingMigrations { get; set; } = new List<MigrationInfo>();

        /// <summary>
        /// Gets or sets the current schema version
        /// </summary>
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target schema version
        /// </summary>
        public string TargetVersion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Migration history
    /// </summary>
    public class MigrationHistory
    {
        /// <summary>
        /// Gets or sets the migration ID
        /// </summary>
        public string MigrationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the migration name
        /// </summary>
        public string MigrationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the migration was applied
        /// </summary>
        public DateTime AppliedAt { get; set; }

        /// <summary>
        /// Gets or sets who applied the migration
        /// </summary>
        public string AppliedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the execution time
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets whether the migration was successful
        /// </summary>
        public bool WasSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the error message if the migration failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Migration types
    /// </summary>
    public enum MigrationType
    {
        /// <summary>
        /// Schema migration
        /// </summary>
        Schema,

        /// <summary>
        /// Data migration
        /// </summary>
        Data,

        /// <summary>
        /// Seed data migration
        /// </summary>
        Seed,

        /// <summary>
        /// Rollback migration
        /// </summary>
        Rollback
    }
} 