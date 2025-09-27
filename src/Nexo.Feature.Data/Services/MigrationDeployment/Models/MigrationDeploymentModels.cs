using System;
using System.Collections.Generic;

namespace Nexo.Feature.Data.Services.MigrationDeployment.Models
{
    /// <summary>
    /// Migration deployment options
    /// </summary>
    public class MigrationDeploymentOptions
    {
        /// <summary>
        /// Whether to create a database backup before deployment
        /// </summary>
        public bool CreateBackup { get; set; } = true;

        /// <summary>
        /// Whether to enable pre-deployment testing
        /// </summary>
        public bool EnablePreDeploymentTesting { get; set; } = true;

        /// <summary>
        /// Whether to enable post-deployment testing
        /// </summary>
        public bool EnablePostDeploymentTesting { get; set; } = true;

        /// <summary>
        /// Whether to enable automatic rollback on failure
        /// </summary>
        public bool EnableAutomaticRollback { get; set; } = true;

        /// <summary>
        /// Maximum deployment timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Whether to run in dry-run mode (validate only)
        /// </summary>
        public bool DryRun { get; set; } = false;
    }

    /// <summary>
    /// Migration deployment result
    /// </summary>
    public class MigrationDeploymentResult
    {
        /// <summary>
        /// Whether the deployment was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Deployment message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// When the deployment started
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// When the deployment completed
        /// </summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// Total execution time
        /// </summary>
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>
        /// Deployment options used
        /// </summary>
        public MigrationDeploymentOptions Options { get; set; } = new MigrationDeploymentOptions();

        /// <summary>
        /// Target migration ID (for single migration deployment)
        /// </summary>
        public string? TargetMigrationId { get; set; }

        /// <summary>
        /// Validation result
        /// </summary>
        public MigrationValidationResult? ValidationResult { get; set; }

        /// <summary>
        /// Migration result
        /// </summary>
        public MigrationResult? MigrationResult { get; set; }

        /// <summary>
        /// Rollback result (if rollback was performed)
        /// </summary>
        public MigrationResult? RollbackResult { get; set; }

        /// <summary>
        /// Applied migrations
        /// </summary>
        public List<MigrationInfo> AppliedMigrations { get; set; } = new List<MigrationInfo>();

        /// <summary>
        /// Final schema version after deployment
        /// </summary>
        public string FinalSchemaVersion { get; set; } = string.Empty;

        /// <summary>
        /// Backup location (if backup was created)
        /// </summary>
        public string? BackupLocation { get; set; }

        /// <summary>
        /// Deployment errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Deployment warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Migration test result
    /// </summary>
    public class MigrationTestResult
    {
        /// <summary>
        /// Whether the tests passed
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Test errors
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Test warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Database backup result
    /// </summary>
    public class DatabaseBackupResult
    {
        /// <summary>
        /// Whether the backup was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Backup message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Backup file path
        /// </summary>
        public string BackupPath { get; set; } = string.Empty;

        /// <summary>
        /// Error message if backup failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
