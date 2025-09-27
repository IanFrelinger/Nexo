using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;
using Nexo.Feature.Data.Services.MigrationDeployment.Testing;
using Nexo.Feature.Data.Services.MigrationDeployment.Backup;
using Nexo.Feature.Data.Services.MigrationDeployment.Execution;

namespace Nexo.Feature.Data.Services.MigrationDeployment.Core
{
    /// <summary>
    /// Automated migration deployment service with validation and testing
    /// </summary>
    public class MigrationDeploymentService
    {
        private readonly IMigrationService _migrationService;
        private readonly IDatabaseProvider _databaseProvider;
        private readonly ILogger<MigrationDeploymentService> _logger;
        private readonly MigrationTestRunner _testRunner;
        private readonly DatabaseBackupService _backupService;
        private readonly MigrationExecutionOrchestrator _executionOrchestrator;

        public MigrationDeploymentService(
            IMigrationService migrationService,
            IDatabaseProvider databaseProvider,
            ILogger<MigrationDeploymentService> logger)
        {
            _migrationService = migrationService ?? throw new ArgumentNullException(nameof(migrationService));
            _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _testRunner = new MigrationTestRunner(_databaseProvider, _migrationService, _logger);
            _backupService = new DatabaseBackupService(_logger);
            _executionOrchestrator = new MigrationExecutionOrchestrator(_migrationService, _logger);
        }

        /// <summary>
        /// Deploy migrations with full validation and testing
        /// </summary>
        /// <param name="options">Deployment options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Deployment result</returns>
        public async Task<MigrationDeploymentResult> DeployMigrationsAsync(
            MigrationDeploymentOptions options,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new MigrationDeploymentResult
            {
                StartedAt = DateTime.UtcNow,
                Options = options
            };

            try
            {
                _logger.LogInformation("Starting migration deployment with options: {@Options}", options);

                // Step 1: Pre-deployment validation
                result.ValidationResult = await _migrationService.ValidateMigrationsAsync(cancellationToken);
                if (!result.ValidationResult.IsValid)
                {
                    result.IsSuccessful = false;
                    result.Message = "Pre-deployment validation failed";
                    result.Errors.AddRange(result.ValidationResult.Errors);
                    return result;
                }

                if (result.ValidationResult.Warnings.Any())
                {
                    result.Warnings.AddRange(result.ValidationResult.Warnings);
                }

                // Step 2: Pre-deployment testing (if enabled)
                if (options.EnablePreDeploymentTesting)
                {
                    var testResult = await _testRunner.RunPreDeploymentTestsAsync(cancellationToken);
                    if (!testResult.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.Message = "Pre-deployment testing failed";
                        result.Errors.AddRange(testResult.Errors);
                        return result;
                    }
                }

                // Step 3: Create backup (if enabled)
                if (options.CreateBackup)
                {
                    var backupResult = await _backupService.CreateDatabaseBackupAsync(cancellationToken);
                    if (!backupResult.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.Message = "Database backup failed";
                        result.Errors.Add(backupResult.ErrorMessage);
                        return result;
                    }
                    result.BackupLocation = backupResult.BackupPath;
                }

                // Step 4: Apply migrations
                var migrationResult = await _executionOrchestrator.ExecuteMigrationsAsync(cancellationToken);
                result.MigrationResult = migrationResult;

                if (!migrationResult.IsSuccessful)
                {
                    result.IsSuccessful = false;
                    result.Message = "Migration application failed";
                    result.Errors.AddRange(migrationResult.FailedMigrations.Select(m => $"Failed to apply {m.Id}: {m.Name}"));
                    
                    // Attempt rollback if enabled
                    if (options.EnableAutomaticRollback)
                    {
                        _logger.LogWarning("Attempting automatic rollback due to migration failures");
                        var rollbackResult = await _executionOrchestrator.RollbackToLastStableStateAsync(cancellationToken);
                        result.RollbackResult = rollbackResult;
                        result.Message += " - Automatic rollback attempted";
                    }
                    
                    return result;
                }

                // Step 5: Post-deployment testing (if enabled)
                if (options.EnablePostDeploymentTesting)
                {
                    var testResult = await _testRunner.RunPostDeploymentTestsAsync(cancellationToken);
                    if (!testResult.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.Message = "Post-deployment testing failed";
                        result.Errors.AddRange(testResult.Errors);
                        
                        // Attempt rollback if enabled
                        if (options.EnableAutomaticRollback)
                        {
                            _logger.LogWarning("Attempting automatic rollback due to post-deployment test failures");
                            var rollbackResult = await _executionOrchestrator.RollbackToLastStableStateAsync(cancellationToken);
                            result.RollbackResult = rollbackResult;
                            result.Message += " - Automatic rollback attempted";
                        }
                        
                        return result;
                    }
                }

                // Step 6: Update deployment metadata
                result.IsSuccessful = true;
                result.Message = $"Successfully deployed {migrationResult.AppliedMigrations.Count} migrations";
                result.AppliedMigrations = migrationResult.AppliedMigrations.ToList();
                result.FinalSchemaVersion = await _migrationService.GetSchemaVersionAsync(cancellationToken);

                _logger.LogInformation("Migration deployment completed successfully: {AppliedCount} migrations applied", 
                    migrationResult.AppliedMigrations.Count);
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Message = $"Deployment failed with exception: {ex.Message}";
                result.Errors.Add(ex.ToString());
                _logger.LogError(ex, "Migration deployment failed");
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
                result.CompletedAt = DateTime.UtcNow;
            }

            return result;
        }

        /// <summary>
        /// Deploy a specific migration with validation
        /// </summary>
        /// <param name="migrationId">Migration ID to deploy</param>
        /// <param name="options">Deployment options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Deployment result</returns>
        public async Task<MigrationDeploymentResult> DeployMigrationAsync(
            string migrationId,
            MigrationDeploymentOptions options,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new MigrationDeploymentResult
            {
                StartedAt = DateTime.UtcNow,
                Options = options,
                TargetMigrationId = migrationId
            };

            try
            {
                _logger.LogInformation("Starting single migration deployment: {MigrationId}", migrationId);

                // Step 1: Validate specific migration
                var validationResult = await _migrationService.ValidateMigrationsAsync(cancellationToken);
                result.ValidationResult = validationResult;

                var targetMigration = validationResult.PendingMigrations.FirstOrDefault(m => m.Id == migrationId);
                if (targetMigration == null)
                {
                    result.IsSuccessful = false;
                    result.Message = $"Migration {migrationId} not found or already applied";
                    return result;
                }

                // Step 2: Create backup (if enabled)
                if (options.CreateBackup)
                {
                    var backupResult = await _backupService.CreateDatabaseBackupAsync(cancellationToken);
                    if (!backupResult.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.Message = "Database backup failed";
                        result.Errors.Add(backupResult.ErrorMessage);
                        return result;
                    }
                    result.BackupLocation = backupResult.BackupPath;
                }

                // Step 3: Apply specific migration
                var migrationResult = await _executionOrchestrator.ExecuteSingleMigrationAsync(migrationId, cancellationToken);
                result.MigrationResult = migrationResult;

                if (!migrationResult.IsSuccessful)
                {
                    result.IsSuccessful = false;
                    result.Message = $"Failed to apply migration {migrationId}";
                    result.Errors.Add(migrationResult.Message);
                    return result;
                }

                // Step 4: Post-deployment testing (if enabled)
                if (options.EnablePostDeploymentTesting)
                {
                    var testResult = await _testRunner.RunPostDeploymentTestsAsync(cancellationToken);
                    if (!testResult.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.Message = "Post-deployment testing failed";
                        result.Errors.AddRange(testResult.Errors);
                        
                        // Attempt rollback if enabled
                        if (options.EnableAutomaticRollback)
                        {
                            _logger.LogWarning("Attempting automatic rollback due to post-deployment test failures");
                            var rollbackResult = await _executionOrchestrator.RollbackToLastStableStateAsync(cancellationToken);
                            result.RollbackResult = rollbackResult;
                            result.Message += " - Automatic rollback attempted";
                        }
                        
                        return result;
                    }
                }

                result.IsSuccessful = true;
                result.Message = $"Successfully deployed migration {migrationId}";
                result.AppliedMigrations = migrationResult.AppliedMigrations.ToList();
                result.FinalSchemaVersion = await _migrationService.GetSchemaVersionAsync(cancellationToken);

                _logger.LogInformation("Single migration deployment completed successfully: {MigrationId}", migrationId);
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Message = $"Deployment failed with exception: {ex.Message}";
                result.Errors.Add(ex.ToString());
                _logger.LogError(ex, "Single migration deployment failed: {MigrationId}", migrationId);
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
                result.CompletedAt = DateTime.UtcNow;
            }

            return result;
        }
    }
}
